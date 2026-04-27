# Calculadora (WPF / .NET)

Calculadora desktop para Windows com **tema claro/escuro**, **modo básico/científico**, **histórico** e **configurações** persistidas.

## Requisitos

- Windows 10+
- [.NET SDK](https://aka.ms/dotnet/download) compatível com o `TargetFramework` do projeto (atualmente `net10.0-windows`)

## Como executar

No PowerShell, na pasta do projeto:

```powershell
dotnet run
```

## Como compilar

```powershell
dotnet build -c Release
```

O executável gerado fica em:

- `bin\Release\net10.0-windows\Calculadora.exe`

## Funcionalidades

- **Modo Básico**: operações comuns (+, −, ×, ÷, % contextual no modo básico, etc.)
- **Modo Científico**: expressões com parênteses, potência (`^`), funções (`sin`, `cos`, `tan`, `ln`, `log`, `sqrt`, `abs`, `exp`, `asin`, `acos`) e **% contextual**
- **Ângulo (Científica)**: graus ou radianos (afeta trigonometria)
- **Histórico**: lista lateral (opcional) com persistência em arquivo
- **Configurações**: janela com abas **Geral** e **Científica**

## Onde ficam as preferências

Os arquivos são salvos em:

- `%AppData%\Calculadora\settings.json`
- `%AppData%\Calculadora\history.json` (quando há histórico)

## Notas sobre o ícone

O ícone da janela é um `DrawingImage` vetorial definido em `App.xaml` (`AppIcon`).

Se você quiser o ícone também no arquivo `.exe` no Explorer, adicione um `.ico` no projeto e configure `ApplicationIcon` no `.csproj`.

## Solução de problemas

### Build falha dizendo que `Calculadora.exe` está em uso

Feche o app antes de compilar, ou finalize o processo `Calculadora` no Gerenciador de Tarefas.