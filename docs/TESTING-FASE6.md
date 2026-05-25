# Guia de teste — Fase 6 (Edit Mode + persistência)

Adiciona o modo de configuração: você pode mover os widgets para onde
quiser e o app lembra entre execuções.

**Risco**: zero. Continuamos rodando em replay ou Test Drive offline.

Tempo: **10 minutos**.

---

## Pré-requisitos

Os mesmos da Fase 5. Se o app está funcionando localmente, está tudo
pronto.

---

## O que mudou

Três coisas:

1. **Hotkey global `Ctrl+Alt+E`** — pressione em qualquer lugar (mesmo
   com iRacing em foco) para alternar entre modo locked (default) e Edit
   Mode.

2. **Edit Mode** — quando ativado:
   - Aparece uma faixa amarela no topo da tela: `EDIT MODE — drag
     widgets to reposition` + `Ctrl+Alt+E to lock`
   - Os widgets (Status, Radar, Relative) ficam **arrastáveis** e
     **redimensionáveis** com o mouse
   - Click-through é DESABILITADO temporariamente para você poder
     interagir — clicar em iRacing por trás do overlay vai bater no
     overlay enquanto em Edit Mode
   - O app continua renderizando mesmo se você Alt+Tab para outro
     programa (você pode posicionar tudo sem iRacing em foco)

3. **Persistência em `%LocalAppData%\iRadar\settings.json`** — quando
   você sai do Edit Mode (Ctrl+Alt+E de novo), as posições e tamanhos
   atuais são salvos. Na próxima execução, os widgets reaparecem no
   mesmo lugar.

---

## Rodar

```powershell
cd $HOME\Documents\iCorre-overlay
git pull
dotnet build iRadar.sln -c Release
dotnet run --project src/iRadar.App -c Release
```

Esperado no console (linhas novas):
```
[settings] no file at C:\Users\<voce>\AppData\Local\iRadar\settings.json — using defaults
...
[overlay] edit mode ON    ← quando você aperta Ctrl+Alt+E
[overlay] edit mode OFF   ← quando aperta de novo
[settings] saved to C:\Users\<voce>\AppData\Local\iRadar\settings.json   ← imediatamente após desligar Edit Mode
```

---

## Checklist de validação

### Edit Mode toggle
- [ ] Com o app rodando e iRacing em replay, aperte `Ctrl+Alt+E`
- [ ] Faixa amarela aparece no topo da tela com o texto "EDIT MODE"
- [ ] Console mostra `[overlay] edit mode ON`
- [ ] Aperte `Ctrl+Alt+E` de novo: faixa some
- [ ] Console mostra `[overlay] edit mode OFF` seguido de `[settings] saved to ...`

### Arrastar widgets em Edit Mode
- [ ] Em Edit Mode, clique e arraste o widget **Status** para outra
      posição na tela
- [ ] Faça o mesmo com **Radar** e **Relative**
- [ ] Redimensione qualquer um pelos cantos
- [ ] Saia do Edit Mode (Ctrl+Alt+E) — widgets ficam onde você deixou
- [ ] Click-through volta a funcionar: cliques sobre os widgets passam
      pro iRacing normalmente

### Visibility toggle por widget (Edit Mode)
- [ ] Em Edit Mode, cada widget mostra no topo um checkbox `Status:
      visible`, `Radar: visible`, `Relative: visible`
- [ ] Desmarque um deles — o conteúdo do widget some, restando só o
      checkbox + "(hidden during racing)"
- [ ] Saia do Edit Mode — o widget desmarcado fica oculto em pista
      (nem o player rectangle aparece para esse widget)
- [ ] Os widgets visíveis continuam funcionando normalmente
- [ ] Volte ao Edit Mode (Ctrl+Alt+E) — o widget oculto reaparece com
      o checkbox para você poder reativá-lo
- [ ] Marque de novo, saia do Edit Mode — widget volta normal
- [ ] Estado de visibilidade persiste entre execuções (gravado no
      `settings.json`)

### Persistência entre execuções
- [ ] Feche o app (Ctrl+C no console ou Alt+F4 no overlay)
- [ ] Abra `%LocalAppData%\iRadar\settings.json` no Notepad — deve
      conter as posições/tamanhos que você definiu:
      ```json
      {
        "version": 1,
        "widgets": {
          "status":   { "id": "status",   "x": 123, "y": 456, ... },
          "radar":    { "id": "radar",    "x": ...,           ... },
          "relative": { "id": "relative", "x": ...,           ... }
        }
      }
      ```
- [ ] Rode `dotnet run --project src/iRadar.App -c Release` de novo
- [ ] Console mostra `[settings] loaded from ... (version 1)`
- [ ] Widgets aparecem nas posições que você havia configurado

### Fora do Edit Mode
- [ ] Comportamento da Fase 5 inalterado: click-through total, widgets
      não interativos, halos no radar funcionando

---

## Reset para defaults

Apaga o `settings.json` e reabre o app:

```powershell
del $env:LOCALAPPDATA\iRadar\settings.json
dotnet run --project src/iRadar.App -c Release
```

Console mostrará `[settings] no file at ... — using defaults` e os
widgets voltam ao layout padrão.

---

## Troubleshooting

### Hotkey não funciona
- O `GetAsyncKeyState` lê o estado global do teclado. Se outro app
  estiver capturando exclusivamente o teclado, pode interferir. Não
  conhecemos casos onde isso acontece com iRacing, mas se acontecer,
  troque para `RegisterHotKey` (Fase 7+).
- Verifique no console se `[overlay] edit mode ON` aparece quando você
  aperta — se sim, a hotkey está funcionando mas talvez a faixa esteja
  fora da tela visível. Tente reset (ver acima).

### Settings não persistem
- Cheque se o arquivo `%LocalAppData%\iRadar\settings.json` existe
  depois de Edit Mode OFF
- Cheque o log: deve aparecer `[settings] saved to ...`
- Se aparecer `[settings] could not save ...`, há problema de
  permissão de escrita — me reporta o erro

### Widget some completamente
- Você pode ter arrastado pra fora da tela visível. Apague o
  `settings.json` para resetar.

---

## Como reportar de volta

- **Tudo OK**: `OK Fase 6` e a gente fecha o MVP. Próximas fases (7+)
  são Validação extensiva e Distribuição/Installer.
- **Algo errado**: cola o output do console + comportamento observado.

---

## O que essa fase prova

✅ Hotkey global funciona  
✅ Edit Mode alterna click-through corretamente  
✅ Widgets ficam arrastáveis em Edit Mode  
✅ Persistência em JSON funciona (save + load + reset)  
✅ Pipeline completo: layout → ImGui → user drag → capture → save → reload

❌ Ainda não tem: configuração visual de cores/opacidade (poderia entrar
   em Fase 7+ junto com um painel de settings), configuração de qual
   widget mostrar/esconder.
