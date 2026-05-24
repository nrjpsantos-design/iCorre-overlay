# Guia de teste — Fase 4 (overlay window)

Este passo prova que a janela transparente do iRadar aparece sobre o iRacing,
fica click-through, some quando você alt-tab, e mostra a telemetria ao vivo.

**Risco**: ainda zero. Continuamos rodando em replay.

Tempo: **5–10 minutos** se você já passou pela Fase 1.

---

## Pré-requisitos

Os mesmos da Fase 1, já validados. Você precisa também:

- iRacing rodando em modo **Windowed Borderless** (não fullscreen exclusive).
  No iRacing: Options → Graphics → Display Mode → **Windowed (Borderless)**.
  Reinicie o iRacing depois de mudar.

> **Por quê**: janelas Win32 transparentes **não renderizam por cima** de
> jogos em fullscreen exclusive. Borderless dá a mesma "experiência de
> fullscreen" e permite o overlay. É o modo padrão da maioria dos sim
> racers que usam overlays.

---

## Passo 1 — Atualizar o build

```powershell
cd $HOME\Documents\iCorre-overlay
git pull
dotnet restore iRadar.sln
dotnet build iRadar.sln -c Release
```

Esperado: `Build succeeded. 0 Error(s)`. Pode haver alguns warnings vindos
da ClickableTransparentOverlay (ignorar).

---

## Passo 2 — iRacing em replay

Mesma sequência da Fase 1:
1. Abrir iRacing
2. **Replays** → carregar uma sessão → **Play**
3. Confirmar que o replay está rodando e a câmera segue um carro

---

## Passo 3 — Rodar o app (não mais só o dumper)

```powershell
dotnet run --project src/iRadar.App -c Release
```

**Esperado** no console:
```
iRadar — starting
Anti-cheat boundary: external process, IRSDK shared memory only.
Close the overlay window (Alt+F4) or press Ctrl+C in this console to stop.

[telemetry] Connecting
[telemetry] InSession
[overlay] starting render loop
[overlay] following iRacing onto monitor 1920x1080 at (1920, 0)
```

> O último `[overlay] following iRacing onto monitor ...` aparece quando o
> app detecta em qual monitor o iRacing está rodando e move a janela
> transparente para esse monitor. Em setup single-monitor as coordenadas
> são `(0, 0)`. Em multi-monitor com iRacing no secundário, você vai ver
> coordenadas como `(1920, 0)`, `(-1920, 0)`, etc.
>
> Se você mover o iRacing entre monitores depois, o overlay segue dentro
> de até 2 segundos (re-check periódico).

E **na tela** (sobreposto ao iRacing): uma pequena janela no canto superior
esquerdo com algo assim:

```
┌─ iRadar — Fase 4 ────────────────────┐
│ Telemetry connected                  │
│ ──────────────────────────────────── │
│ Track    Circuito de Navarra         │
│ Tick     14572                       │
│ Cars     64                          │
│ State    replay                      │
└──────────────────────────────────────┘
```

A janela tem fundo transparente (sem retângulo opaco atrás), está sempre no
topo, e o `Tick` aumenta a cada ~16ms.

---

## Checklist de validação

- [ ] A janela aparece sobre o iRacing em replay
- [ ] O fundo é transparente (você vê o iRacing através dele, só o texto e o
      título da janela ImGui são visíveis)
- [ ] Você consegue **clicar através** da janela (ex.: clique no botão de
      pause do iRacing onde a janela está sobreposta — o iRacing responde)
- [ ] Quando você Alt+Tab para outra aplicação (Chrome, Notepad, etc.) o
      overlay **some** — `HostProcessDetector` viu que o iRacing não está
      mais em foreground
- [ ] Quando volta para o iRacing, o overlay **reaparece**
- [ ] O campo `Tick` aumenta em tempo real conforme o replay avança
- [ ] `Cars` mostra o número correto (no replay do Navarra: 64)
- [ ] `State` mostra `replay`
- [ ] Console mostra `[telemetry] InSession` (não fica preso em `Connecting`)

---

## Bonus — Test Drive ao vivo

Para validar que `state=on-track` funciona quando você está pilotando de
verdade (continua zero-risco porque é offline):

1. iRacing → Single Race → **Test Drive** em qualquer pista
2. Entrar em pista
3. O overlay deve mostrar:
   - `State: on-track`
   - `Spotter: Clear` (e mudar para `CarLeft`/`CarRight` se você for chegar
     perto de outro carro — embora em Test Drive sozinho não tenha mais
     ninguém na pista; para spotter dinâmico, AI Race offline é melhor)

---

## Onde olhar diagnósticos

Além do console, o app sempre escreve em:

```
%LocalAppData%\iRadar\debug.log
```

Abre com:
```powershell
notepad $env:LOCALAPPDATA\iRadar\debug.log
```

Se o console estiver mudo ou se você quiser revisar histórico de execuções,
esse arquivo tem todas as linhas que vão para o console + erros não tratados
com stack trace.

---

## Troubleshooting

### A janela não aparece
- iRacing está em **fullscreen exclusive**? Mude para **Windowed Borderless**
  em Options → Graphics → Display Mode.
- O console mostra `[telemetry] Connecting` indefinidamente? Veja
  troubleshooting da [Fase 1](TESTING-FASE1.md).

### A janela aparece mas o fundo é preto/cinza em vez de transparente
A composição de janela layered tem dois caminhos no Windows. Em modos
estranhos de DWM (acessibilidade, contraste alto, alguns drivers de placa
antigos), CTO pode renderizar com fundo opaco. Tente desabilitar "transparency
effects" em Configurações do Windows → Personalização → Cores e veja se
muda. Me reporta com a versão do Windows e da GPU.

### A janela fica visível mesmo com Alt+Tab
- O `HostProcessDetector` checa `Process.ProcessName`. Veja no Task Manager
  qual o nome exato do processo do iRacing nesta máquina:
  - Se for diferente de `iRacingSim64DX11` ou `iRacingSim64DX12`, me manda o
    nome — adiciono na lista `IRacingProcessNames.All`.

### `dotnet run` falha com "Could not load file or assembly cimgui"
ClickableTransparentOverlay traz `cimgui.dll` nativa. Se o NuGet não copiou
para `bin/Release/net8.0-windows/`, rode:
```powershell
dotnet publish src/iRadar.App -c Release -o publish
cd publish
.\iRadar.exe
```
Publish força o copy das nativas.

### O Tick não muda mesmo com replay rodando
- O dumper da Fase 1 funciona? Se sim, o problema é no overlay (me reporta).
- Se nem o dumper funciona mais, voltou ao caso da Fase 1 — veja aquele
  troubleshooting primeiro.

---

## Como reportar de volta

- **Funcionou**: me manda `OK Fase 4` e sigo para Fase 5 (widgets reais —
  radar circular, spotter visual, painel relative).
- **Falhou**: console + comportamento visual descrito. Screenshot da tela
  ajuda muito se tiver dúvida sobre transparência/posicionamento.

---

## O que essa fase prova

✅ Janela layered transparente funciona  
✅ Click-through funciona  
✅ Always-on-top funciona  
✅ Detecção de foreground funciona  
✅ Pipeline completo: IRSDK → engine → buffer → render  
✅ Performance adequada (60 FPS sem stutter)  

❌ Ainda não prova: widgets visuais do radar (Fase 5), edit mode (Fase 6),
performance < 2% CPU sob carga (Fase 7).
