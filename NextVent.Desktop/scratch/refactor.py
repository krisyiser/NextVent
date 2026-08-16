import re

file_path = r'c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\NextVent.Desktop\Services\Implementations\SaleService.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    code = f.read()

# Replace fields and constructor
code = code.replace('private readonly AppDbContext _ctx;', 'private readonly IDbContextFactory<AppDbContext> _contextFactory;')
code = code.replace('public SaleService(AppDbContext ctx) => _ctx = ctx;', 'public SaleService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;')

# For every method that uses _ctx, we need to inject: using var _ctx = await _contextFactory.CreateDbContextAsync();
# It's easier to manually do it for the few methods.
# Actually let's just do it manually with multi_replace.
