# Guia de teste — Fase 1 (zero-risco)

Este guia te leva do "tenho o repo no Windows" até "vejo telemetria real do
iRacing aparecendo no console". Tudo é feito com **iRacing em modo Replay**, o
que significa risco zero de banimento — o jogo está reproduzindo uma gravação,
não rodando uma sessão online.

Tempo estimado: **20–30 minutos**, sendo a maior parte download do .NET SDK.

---

## Pré-requisitos no Windows

| Item | Como verificar | Onde obter |
|---|---|---|
| Windows 10 ou 11 | `winver` | — |
| Git instalado | `git --version` no PowerShell | https://git-scm.com/download/win |
| .NET 8 SDK (não só Runtime!) | `dotnet --list-sdks` deve mostrar `8.x.x` | https://dotnet.microsoft.com/download/dotnet/8.0 → seção "SDK 8.0" → "x64 Windows Installer" |
| iRacing instalado e com pelo menos 1 replay salvo | abrir o iRacing | — |

> **Dica .NET**: instale o **SDK x64** (não o "Runtime" e não o "ASP.NET Core
> Runtime"). Após instalar, abra um **novo** PowerShell — o instalador atualiza
> o PATH só em sessões novas.

---

## Passo 1 — Clonar e fazer build

Abra um PowerShell e rode:

```powershell
cd $HOME\Documents
git clone git@github.com:nrjpsantos-design/iCorre-overlay.git
cd iCorre-overlay
dotnet restore iRadar.sln
dotnet build iRadar.sln -c Release
```

> Se você não tem chave SSH configurada no GitHub, use HTTPS:
> `git clone https://github.com/nrjpsantos-design/iCorre-overlay.git`

**Esperado**: a última linha do `dotnet build` deve dizer
`Build succeeded.` e mostrar `0 Errors` (warnings sobre packages sem
PackageReadme são ok).

Se algo falhar aqui, copia o erro e me manda — provavelmente é só uma versão
de pacote pra ajustar.

---

## Passo 2 — Abrir um replay no iRacing

1. Abrir o iRacing.
2. No menu superior, ir em **Replays** (não Race, não Hosted).
3. Selecionar qualquer sessão antiga sua. Se não tem uma sua, qualquer replay
   de demo serve.
4. Clicar **Play**. Aguardar a sessão carregar e a câmera começar a se mover.

**Por que replay é zero-risco**: o iRacing está em modo "playback" — não há
sessão ao vivo, não há rating em jogo, não há servidor oficial envolvido.
Você pode ter qualquer aplicação lendo a telemetria sem consequência.

---

## Passo 3 — Rodar o dumper de telemetria

Com o replay rodando no iRacing, **deixe o iRacing aberto** e volte para o
PowerShell (Alt+Tab funciona mesmo com o iRacing em windowed-borderless):

```powershell
cd $HOME\Documents\iCorre-overlay
dotnet run --project tests/iRadar.Replay -c Release
```

**Esperado** — o console mostra algo assim em alguns segundos:

```
iRadar — Fase 1 telemetry dumper
Open iRacing in replay mode for zero-risk testing.
Ctrl+C to stop.

[state] Connecting
[state] InSession
tick=  123456 playerIdx= 37 camIdx= 37 speed= 247.3km/h lap=  3 lapPct=0.481 cars=64 proximity=n/a          track="Circuito de Navarra" [replay]
tick=  123472 playerIdx= 37 camIdx= 37 speed= 251.8km/h lap=  3 lapPct=0.482 cars=64 proximity=n/a          track="Circuito de Navarra" [replay]
...
```

A cada ~100ms uma linha nova é impressa enquanto o replay avança.

`Ctrl+C` para parar.

> **`proximity=n/a` e `[replay]` são esperados em modo replay!** Veja a
> próxima seção.

---

## Por que `proximity` e o estado mudam em replay vs live

O iRacing trata replay e live como contextos diferentes. Algumas variáveis
de telemetria só fazem sentido quando há um piloto vivo dirigindo:

| Variável iRacing | Em REPLAY | Em LIVE (Test Drive, Race, etc.) |
|---|---|---|
| `CarLeftRight` (proximity) | sempre `Off` (mostrado como `n/a` no dumper) | `Clear`/`CarLeft`/`CarRight`/etc. conforme cenário |
| `IsOnTrack` | sempre `false` | `true` quando você está na pista, `false` em pit/garage |
| `IsReplayPlaying` | `true` | `false` |

O dumper mostra três estados possíveis no fim da linha:

- `[replay]` — iRacing está em playback de uma gravação
- `[on-track]` — você está pilotando uma sessão e está em pista
- `[pit/garage]` — você está em sessão mas no box ou garagem

Em modo replay, o radar engine ainda terá os dados que importam
(`CarIdxLapDistPct` por carro, ou seja, onde cada um está no traçado), mas
o **spotter visual** só funciona quando você está realmente pilotando uma
sessão. Isto é uma limitação do iRacing, não do app.

---

## Checklist de validação

Marque mentalmente:

- [ ] State transita: `Connecting` → `InSession` (em replay) ou `InCar`
      (em sessão ao vivo na pista)
- [ ] `playerIdx` é um número ≥ 0 (não `-1`)
- [ ] `camIdx` é um número ≥ 0 (em replay segue a câmera; em live geralmente
      iguala `playerIdx`)
- [ ] `speed` está em `km/h`, valor realista (zero quando parado, > 0 quando
      o carro se move)
- [ ] `lap` cresce conforme o replay avança
- [ ] `lapPct` está entre `0.000` e `1.000` e está aumentando
- [ ] `cars` é o número total de carros na sessão (no replay do Navarra do
      João foram 64 — bate)
- [ ] `track` mostra o nome correto do circuito
- [ ] O label final no fim da linha bate com o que está rolando no iRacing:
      `[replay]` quando assistindo replay, `[on-track]`/`[pit/garage]`
      quando dirigindo
- [ ] Ao fechar a janela do replay (no menu), `[state] Disconnected`
      aparece — e ao abrir o replay de novo, reconecta automaticamente
- [ ] **Bonus (opcional)**: entrar numa sessão Test Drive offline (Single
      Race → Test Drive em qualquer pista) e confirmar que `proximity`
      reflete carros próximos quando você dirige perto deles. Isto valida
      o spotter end-to-end e é zero-risco também (offline)

---

## Troubleshooting

### `dotnet` não encontrado
SDK não instalado, ou PowerShell aberto antes da instalação. Feche e reabra
o PowerShell.

### `dotnet --list-sdks` mostra só `6.x` ou `7.x`
Você instalou um SDK antigo. Baixe a versão 8.x específica do link na tabela
acima.

### `[state]` fica em `Connecting` indefinidamente
- iRacing está **aberto** mas **sem sessão carregada**? Carregue um replay
  (Passo 2).
- iRacing está fechado? Abra-o.
- Você abriu o iRacing **depois** de iniciar o dumper? Não é necessário, mas
  espere uns 2–3 segundos — o dumper tenta reconectar automaticamente.

### State chega em `InCar` mas todos os valores estão em zero
Provável bug no parser binário. Me manda o output completo e o nome da pista
do replay para eu reproduzir.

### Erro "MemoryMappedFile.OpenExisting: FileNotFoundException"
O nome do MMF do iRacing mudou em alguma atualização recente, ou você está
rodando o dumper como Administrador mas o iRacing rodando como usuário
normal (ou vice-versa). Solução: rodar ambos no mesmo nível de privilégio
(o normal é usuário comum, sem "Run as Administrator").

### O número de carros está estranho ou faltam nomes de pilotos
O parser de YAML pega só uma fatia dos drivers. Manda o `tick`, `cars`, e o
print do início do dumper que eu vejo se há regressão.

### `dotnet build` falha com erro de "NuGet package not found"
Provavelmente firewall / proxy corporativo (Taya?). Tente:
```powershell
$env:DOTNET_NUGET_SIGNATURE_VERIFICATION=$false
dotnet nuget locals all --clear
dotnet restore iRadar.sln --force
```

### Antivírus marca `iRadar.exe` como suspeito
O `iRadar.Replay.exe` é gerado localmente, sem assinatura digital — isto é
esperado em Fase 1. Adicione o `bin\Release\` do projeto à exclusão do AV
ou rode via `dotnet run` (não via .exe direto) que executa o binário
através do host do .NET (raramente bloqueado).

---

## Como reportar de volta

Se **tudo funcionou**: ótimo, me manda só `OK` que sigo para Fase 4.

Se **algo deu errado**: copia/cola pelo menos:
- O comando que rodou
- A saída do PowerShell (últimas ~30 linhas)
- O que aparecia no iRacing quando o dumper estava rodando (track, replay
  com câmera em carro vs. live, etc.)

Eu trato isso como sinal de bug e diagnostico daqui.

---

## O que esse teste prova (e o que NÃO prova)

**Prova**:
- IRSDK shared memory abre corretamente no Windows real
- Header e variáveis são parseados sem corromper
- O TelemetrySource consegue manter loop a 60Hz sem crash
- Reconexão funciona quando iRacing fecha e reabre
- Parser de YAML extrai track e drivers corretos

**Não prova** (ainda):
- Janela transparente aparece sobre o iRacing (Fase 4)
- Radar visual mostra dots na posição certa (Fase 5)
- Performance < 2% CPU sob carga real (Fase 7)
- Funciona em sessão online (Fase 10, e só após maturidade comprovada)

Por isso a sequência continua sendo: **valida Fase 1 → segue Fase 4 →
valida Fase 4 → segue Fase 5 → …**
