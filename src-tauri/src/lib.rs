use tauri::{AppHandle, WebviewUrl, WebviewBuilder, Manager};
use std::sync::{Mutex, Arc};
use axum::{
    routing::get, 
    Router,
    extract::{ws::{Message as WsMessage, WebSocket, WebSocketUpgrade}, State},
    response::IntoResponse
};
use tower_http::cors::CorsLayer;
use tower_http::services::ServeDir;
use base64::Engine;
use tokio::sync::broadcast;
use bcrypt::{hash, verify, DEFAULT_COST};

struct AppState {
    tx: broadcast::Sender<String>,
}

struct WhatsappState {
  open: bool,
  width: f64,
  pinned: bool,
  sidebar_position: String,
}

async fn apply_layout(app: &AppHandle, open: bool, width: f64, _pinned: bool, sidebar_position: &str) -> Result<(), String> {
  println!("apply_layout: open={}, width={}, sidebar_position={}", open, width, sidebar_position);
  let app_clone = app.clone();
  let sidebar_pos = sidebar_position.to_string();
  
  app.run_on_main_thread(move || {
    if let Some(window) = app_clone.get_window("main") {
      let size = window.inner_size().unwrap();
      let scale = window.scale_factor().unwrap();
      let width_logical = size.width as f64 / scale;
      let height_logical = size.height as f64 / scale;

      // The main webview always remains full screen. Next.js handles internal margins.
      if let Some(main_webview) = app_clone.get_webview("main") {
        let _ = main_webview.set_bounds(tauri::Rect {
          position: tauri::Position::Logical(tauri::LogicalPosition::new(0.0, 0.0)),
          size: tauri::Size::Logical(tauri::LogicalSize::new(width_logical, height_logical)),
        });
      }

      if !open {
        // HIDE WhatsApp child webview by moving it off-screen and shrinking size to 0
        if let Some(wa_webview) = app_clone.get_webview("whatsapp") {
          let _ = wa_webview.set_bounds(tauri::Rect {
            position: tauri::Position::Logical(tauri::LogicalPosition::new(-2000.0, 0.0)),
            size: tauri::Size::Logical(tauri::LogicalSize::new(0.0, height_logical)),
          });
        }
      } else {
        // Calculate bounds based on sidebar position
        let (wa_x, wa_y, wa_w, wa_h) = match sidebar_pos.as_str() {
            "top" => (0.0, 64.0, width, height_logical - 64.0),
            "bottom" => (0.0, 0.0, width, height_logical - 64.0),
            "right" => (0.0, 0.0, width, height_logical),
            _ => (80.0, 0.0, width, height_logical), // "left" is default
        };

        // Update or create WhatsApp child webview
        if let Some(wa_webview) = app_clone.get_webview("whatsapp") {
          let _ = wa_webview.set_bounds(tauri::Rect {
            position: tauri::Position::Logical(tauri::LogicalPosition::new(wa_x, wa_y)),
            size: tauri::Size::Logical(tauri::LogicalSize::new(wa_w, wa_h)),
          });
        } else {
          let wa_builder = WebviewBuilder::new("whatsapp", WebviewUrl::External("https://web.whatsapp.com/".parse().unwrap()))
            .user_agent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

          let _ = window.add_child(
            wa_builder,
            tauri::LogicalPosition::new(wa_x, wa_y),
            tauri::LogicalSize::new(wa_w, wa_h),
          );
        }
      }
    }
  }).map_err(|e| format!("Failed to run on main thread: {}", e))?;
  Ok(())
}

#[tauri::command]
async fn set_whatsapp_layout(
  app: AppHandle,
  state: tauri::State<'_, Mutex<WhatsappState>>,
  open: bool,
  width: f64,
  pinned: bool,
  sidebar_position: String,
) -> Result<(), String> {
  println!("set_whatsapp_layout command: open={}, width={}, pinned={}, sidebar_position={}", open, width, pinned, sidebar_position);
  // Update state inside lock
  {
    let mut s = state.lock().unwrap();
    s.open = open;
    s.width = width;
    s.pinned = pinned;
    s.sidebar_position = sidebar_position.clone();
  }
  apply_layout(&app, open, width, pinned, &sidebar_position).await
}

#[tauri::command]
async fn send_whatsapp_message(app: AppHandle, phone: String, text: String) -> Result<(), String> {
  if let Some(wa_webview) = app.get_webview("whatsapp") {
    let url = format!("https://web.whatsapp.com/send?phone={}&text={}", phone, urlencoding::encode(&text));
    let _ = wa_webview.eval(&format!("window.location.href = '{}';", url));
  }
  Ok(())
}

#[tauri::command]
async fn backup_database(app: AppHandle) -> Result<String, String> {
  let app_data_dir = app.path().app_data_dir().map_err(|e| e.to_string())?;
  let db_path = app_data_dir.join("app.db");
  
  if !db_path.exists() {
    return Err("Database file not found".into());
  }

  let backups_dir = app_data_dir.join("backups");
  std::fs::create_dir_all(&backups_dir).map_err(|e| format!("Failed to create backups directory: {}", e))?;

  let timestamp = std::time::SystemTime::now()
    .duration_since(std::time::UNIX_EPOCH)
    .unwrap()
    .as_secs();
    
  let backup_filename = format!("app_backup_{}.db", timestamp);
  let backup_path = backups_dir.join(&backup_filename);

  std::fs::copy(&db_path, &backup_path).map_err(|e| format!("Failed to copy database: {}", e))?;

  log::info!("Database backed up to {:?}", backup_path);

  Ok(backup_path.to_string_lossy().to_string())
}

#[tauri::command]
async fn save_audit_image(app: AppHandle, event_id: String, base64_image: String) -> Result<String, String> {
  let app_data_dir = app.path().app_data_dir().map_err(|e| e.to_string())?;
  let audits_dir = app_data_dir.join("audits");
  std::fs::create_dir_all(&audits_dir).map_err(|e| e.to_string())?;
  
  // Remove data:image/png;base64, prefix if present
  let b64 = if base64_image.contains(",") {
      base64_image.split(",").last().unwrap_or(&base64_image).to_string()
  } else {
      base64_image
  };
  
  let decoded = base64::engine::general_purpose::STANDARD.decode(b64).map_err(|e| e.to_string())?;
  let file_path = audits_dir.join(format!("{}.png", event_id));
  
  std::fs::write(&file_path, decoded).map_err(|e| e.to_string())?;
  Ok(file_path.to_string_lossy().to_string())
}

#[tauri::command]
async fn print_receipt(buffer: Vec<u8>, is_reprint: bool) -> Result<(), String> {
  let mut final_buffer = buffer;
  if is_reprint {
      // Inyectar marca de agua
      let mut watermark = b"\x1b\x21\x30*** COPIA DE SEGURIDAD ***\n\n".to_vec();
      watermark.append(&mut final_buffer);
      final_buffer = watermark;
  }
  
  // Como no hay hardware de impresora física, simulamos la impresión guardando en un log
  std::fs::write("printer_log.bin", final_buffer).map_err(|e| e.to_string())?;
  Ok(())
}

#[tauri::command]
async fn imprimir_comprobante_fiscal(uuid: String) -> Result<(), String> {
  let ticket_data = format!("\n--- COMPROBANTE FISCAL ---\nUUID: {}\n--------------------------\n", uuid);
  // Simular impresión de comprobante fiscal guardando en un log
  std::fs::write(format!("fiscal_receipt_{}.txt", uuid), ticket_data).map_err(|e| e.to_string())?;
  Ok(())
}

#[tauri::command]
async fn broadcast_cart(app: AppHandle, cart_json: String) -> Result<(), String> {
  if let Some(state) = app.try_state::<Arc<AppState>>() {
      let _ = state.tx.send(cart_json);
  }
  Ok(())
}

#[tauri::command]
fn get_local_ip() -> String {
    local_ip_address::local_ip()
        .map(|ip| ip.to_string())
        .unwrap_or_else(|_| "127.0.0.1".to_string())
}

#[tauri::command]
fn hash_secret(secret: String) -> Result<String, String> {
    hash(secret, DEFAULT_COST).map_err(|e| format!("Hashing failed: {}", e))
}

#[tauri::command]
fn verify_secret(secret: String, hashed: String) -> Result<bool, String> {
    verify(secret, &hashed).map_err(|e| format!("Verification failed: {}", e))
}

#[tauri::command]
async fn close_splashscreen(app: tauri::AppHandle) -> Result<(), String> {
    if let Some(splash) = app.get_window("splashscreen") {
        let _ = splash.close();
    }
    if let Some(main) = app.get_window("main") {
        let _ = main.show();
        let _ = main.set_focus();
    }
    Ok(())
}

async fn ws_handler(
    ws: WebSocketUpgrade,
    State(state): State<Arc<AppState>>,
) -> impl IntoResponse {
    ws.on_upgrade(|socket| handle_socket(socket, state))
}

async fn handle_socket(mut socket: WebSocket, state: Arc<AppState>) {
    let mut rx = state.tx.subscribe();
    while let Ok(msg) = rx.recv().await {
        if socket.send(WsMessage::Text(msg)).await.is_err() {
            break;
        }
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
  std::panic::set_hook(Box::new(|panic_info| {
    log::error!("A fatal panic occurred: {:?}", panic_info);
  }));

  tauri::Builder::default()
    .plugin(tauri_plugin_log::Builder::default().build())
    .plugin(tauri_plugin_sql::Builder::default().build())
    .plugin(tauri_plugin_http::init())
    .plugin(tauri_plugin_fs::init())
    .invoke_handler(tauri::generate_handler![
      set_whatsapp_layout, 
      send_whatsapp_message,
      backup_database,
      save_audit_image,
      print_receipt,
      imprimir_comprobante_fiscal,
      broadcast_cart,
      get_local_ip,
      hash_secret,
      verify_secret,
      close_splashscreen
    ])
    .setup(|app| {
      let splash_app_handle = app.handle().clone();
      tauri::async_runtime::spawn(async move {
          tokio::time::sleep(std::time::Duration::from_secs(5)).await;
          if let Some(splash) = splash_app_handle.get_window("splashscreen") {
              let _ = splash.close();
          }
          if let Some(main) = splash_app_handle.get_window("main") {
              let _ = main.show();
              let _ = main.set_focus();
          }
      });

      // 8. Base Offline Pre-cargada (Seeding)
      if let Ok(app_data_dir) = app.path().app_data_dir() {
        let db_path = app_data_dir.join("app.db");
        let _ = std::fs::remove_file(&db_path);
        
        let mut log_content = String::new();
        log_content.push_str(&format!("db_path: {:?}\n", db_path));
        
        if let Ok(resource_path) = app.path().resolve("productos_base.db", tauri::path::BaseDirectory::Resource) {
          log_content.push_str(&format!("resource_path: {:?}\n", resource_path));
          if resource_path.exists() {
            let _ = std::fs::create_dir_all(&app_data_dir);
            match std::fs::copy(&resource_path, &db_path) {
                Ok(_) => log_content.push_str("Successfully copied database\n"),
                Err(e) => log_content.push_str(&format!("Failed to copy database: {:?}\n", e)),
            }
          } else {
            log_content.push_str("Resource path does not exist\n");
          }
        } else {
          log_content.push_str("Failed to resolve resource path\n");
        }
        let _ = std::fs::write(app_data_dir.join("copy_log.txt"), log_content);
      }

      let (tx, _rx) = broadcast::channel(100);
      let app_state = Arc::new(AppState { tx });
      app.manage(app_state.clone());

      // Spawn Axum Server for LAN Sync
      tauri::async_runtime::spawn(async move {
          let axum_app = Router::new()
              .route("/api/ping", get(|| async { "pong" }))
              .route("/ws", get(ws_handler))
              .nest_service("/", ServeDir::new("../out")) // Serve exported Next.js app
              .layer(CorsLayer::permissive())
              .with_state(app_state);

          if let Ok(listener) = tokio::net::TcpListener::bind("0.0.0.0:8080").await {
              println!("Axum Server listening on 0.0.0.0:8080");
              let _ = axum::serve(listener, axum_app).await;
          } else {
              eprintln!("Failed to bind Axum server to port 8080");
          }
      });

      app.manage(Mutex::new(WhatsappState {
        open: false,
        width: 450.0,
        pinned: false,
        sidebar_position: "left".to_string(),
      }));

      let app_handle = app.handle().clone();
      if let Some(window) = app.get_window("main") {
        window.on_window_event(move |event| {
          if let tauri::WindowEvent::Resized(_) = event {
            let state = app_handle.state::<Mutex<WhatsappState>>();
            let s = state.lock().unwrap();
            let open = s.open;
            let width = s.width;
            let pinned = s.pinned;
            let sidebar_pos = s.sidebar_position.clone();
            drop(s);

            let app_clone = app_handle.clone();
            tauri::async_runtime::spawn(async move {
              let _ = apply_layout(&app_clone, open, width, pinned, &sidebar_pos).await;
            });
          }
        });
      }
      Ok(())
    })
    .run(tauri::generate_context!())
    .expect("error while running tauri application");
}
