import { fetch } from '@tauri-apps/plugin-http';
import { writeTextFile, BaseDirectory } from '@tauri-apps/plugin-fs';
import AppDatabase from './database';

const URL_API = "https://sandbox-api.facturama.mx/api/v3/cfdi";
// TODO: En producción cambiar a: "https://api.facturama.mx/api/v3/cfdi"

export async function enviarAlSAT(jsonCfdi: any, apiUser: string, apiSecret: string) {
  // Autenticación Basic requerida por Facturama
  const credenciales64 = btoa(`${apiUser}:${apiSecret}`);

  const respuesta = await fetch(URL_API, {
    method: 'POST',
    headers: {
      'Authorization': `Basic ${credenciales64}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(jsonCfdi)
  });

  const data = await respuesta.json();

  if (!respuesta.ok) {
    // Manejo de errores nativos del SAT devueltos por Facturama
    throw new Error(data.Message || "Error desconocido en la validación del SAT");
  }

  return {
    uuid: data.Complement?.TimbreFiscalDigital?.Uuid,
    id: data.Id,
    pdfUrl: data.PdfUrl,
    xmlBase64: data.XmlBase64
  };
}

export async function procesarExitoFiscal(folioVenta: string, respuestaSat: any) {
  // 1. Guardar de forma física el XML localmente
  const xmlDecodificado = atob(respuestaSat.xmlBase64);
  
  try {
    await writeTextFile(`facturas_emitidas/${folioVenta}.xml`, xmlDecodificado, {
      baseDir: BaseDirectory.Document
    });
  } catch (err) {
    console.error("Error guardando XML localmente. Intentando ruta alternativa o ignorando...", err);
  }

  // 2. Actualizar el estado en SQLite local
  const db = await AppDatabase.getInstance();
  await db.execute(
    "UPDATE sales SET estado_fiscal = 'TIMBRADO', uuid_sat = ? WHERE id = ?", 
    [respuestaSat.uuid, folioVenta]
  );

  // 3. Mandar comando a la impresora térmica (Ticketera)
  // Mediante tu bridge ESC/POS de Rust, imprimes un mini comprobante con el UUID del SAT
  const { invoke } = await import('@tauri-apps/api/core');
  await invoke("imprimir_comprobante_fiscal", { uuid: respuestaSat.uuid });
}
