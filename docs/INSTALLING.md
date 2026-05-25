# Instalando o iRadar

Guia para o usuário final — não precisa de PowerShell, git ou .NET SDK.

---

## Requisitos

- **Windows 10 ou 11** (x64)
- **iRacing instalado** com pelo menos uma sessão (replay ou live)

Você **não** precisa instalar o .NET separadamente — o instalador
já vem com todo o runtime embutido (~80 MB).

---

## Passo a passo

### 1. Baixar o instalador

Vá para a página de releases:

> https://github.com/nrjpsantos-design/iCorre-overlay/releases/latest

E baixe o arquivo `iRadar-win-Setup.exe`.

### 2. Executar o instalador

Dê duplo-clique no `iRadar-win-Setup.exe`.

> **Aviso do Windows SmartScreen**: como o instalador ainda não é
> assinado digitalmente (custo de certificado EV é alto para um projeto
> open-source pessoal), o Windows vai mostrar uma tela azul "Windows
> protegeu seu PC".
>
> 1. Clique em **"Mais informações"**
> 2. Clique em **"Executar mesmo assim"**
>
> Isto é one-time — o app instalado em si não dispara essa tela.
> Você pode auditar o código-fonte completo nesse mesmo repositório
> antes de instalar se preferir.

O instalador roda silenciosamente em ~5 segundos e cria:
- Atalho no **Menu Iniciar** → "iRadar"
- Pasta de instalação em `%LocalAppData%\iRadar`
- Pasta de dados (settings, logs) em `%LocalAppData%\iRadar`

### 3. Configuração inicial do iRacing

Para que o overlay apareça por cima do iRacing, o jogo precisa estar
em modo **Windowed Borderless** (não fullscreen exclusive):

1. Abra o iRacing
2. **Options** → **Graphics** → **Display Mode** → **Windowed (Borderless)**
3. Reinicie o iRacing

### 4. Usar o iRadar

1. Abra o iRadar pelo Menu Iniciar
2. Abra o iRacing e entre numa sessão (replay, Test Drive, AI Race, etc.)
3. Os widgets aparecem automaticamente sobre o iRacing
4. `Ctrl+Alt+E` ativa **Edit Mode** para arrastar widgets ou esconder
   alguns

---

## Atualizações

O iRadar verifica automaticamente por novas versões a cada inicialização:

- Se houver atualização disponível, ela é baixada **em background**
  durante a sessão atual
- A nova versão é **instalada apenas quando você fecha o app**
- **Nunca interrompe uma corrida** com restart inesperado

Se preferir desativar updates automáticos, edite
`%LocalAppData%\iRadar\settings.json` e adicione (não implementado ainda
— pedir se quiser).

---

## Onde ficam meus dados

| O que | Onde |
|---|---|
| Posições/visibility dos widgets | `%LocalAppData%\iRadar\settings.json` |
| Log de diagnóstico | `%LocalAppData%\iRadar\debug.log` |
| Binários do app | `%LocalAppData%\iRadar\current\` |

Para resetar tudo, feche o app e apague `settings.json`.

---

## Desinstalando

**Painel de Controle** → **Programas e Recursos** → procure por
**"iRadar"** → Desinstalar.

A pasta `%LocalAppData%\iRadar` com logs e settings é mantida —
apague manualmente se quiser uma limpeza completa.

---

## Política anti-cheat

O iRadar é um **processo externo** ao iRacing. Ele lê telemetria
através da API pública oficial (`Local\IRSDKMemMapFileName`,
documentada pela própria iRacing no IRSDK SDK). Não há:

- ❌ DLL injection no processo do iRacing
- ❌ Hooks em DirectX, Win32, ou qualquer API do jogo
- ❌ Modificação de inputs do piloto
- ❌ Leitura de memória do processo do iRacing
- ❌ Automação de pilotagem

A mesma abordagem é usada por **Garage61, iOverlay, SimHub, CrewChief,
VRS Coach, Kapps, JRT Telemetry, iRon** e dezenas de outros overlays
tolerados pela iRacing há anos.

Mais detalhes técnicos:
[README.md](../README.md#anti-cheat-posture).

---

## Problemas comuns

### O overlay não aparece
- iRacing está em fullscreen exclusive? Mude para Windowed Borderless
- iRacing está rodando? O overlay só aparece com o jogo em foco

### Antivírus bloqueando
- O binário não é assinado (Windows mostra SmartScreen warning na
  instalação). Isso pode disparar falso-positivo em alguns antivírus.
- Adicione `%LocalAppData%\iRadar\current\iRadar.exe` à exclusão do
  seu AV se necessário.

### Edit Mode não responde a Ctrl+Alt+E
- Verifique se o iRadar está rodando (atalho no menu iniciar, ou Task
  Manager mostra `iRadar.exe`)
- O `Ctrl+Alt+E` funciona como hotkey global — mesmo com iRacing em
  foco. Se ainda não funcionar, abra um issue.

### O radar não mostra carros próximos
- Em replay, o `CarLeftRight` do iRacing fica `Off` — o radar mostra
  posições longitudinais OK mas o lateral é heurístico
- Em sessão ao vivo, o spotter funciona normal (carro mais próximo
  ganha posição lateral real)

---

## Reportando bugs

Abra uma issue no GitHub com:
- Versão do iRadar (visível no widget Status ou em `debug.log`)
- Versão do Windows
- Última seção do `%LocalAppData%\iRadar\debug.log`
- Descrição do que esperava vs. o que aconteceu
