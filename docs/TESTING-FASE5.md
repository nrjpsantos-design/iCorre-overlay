# Guia de teste — Fase 5 (widgets reais)

Substituímos o HelloWidget placeholder pelos três widgets de produção:
radar circular, spotter lateral e painel Relative. Esta fase é
exclusivamente validação visual — nenhum risco novo.

Tempo: **10 minutos** se o overlay da Fase 4 já está funcionando.

---

## Pré-requisitos

Os mesmos da Fase 4 (iRacing em Windowed Borderless, .NET 8 SDK, etc.).
Se a Fase 4 estava OK pra você, está tudo pronto.

---

## Rodar

```powershell
cd $HOME\Documents\iCorre-overlay
git pull
dotnet build iRadar.sln -c Release
dotnet run --project src/iRadar.App -c Release
```

Com iRacing aberto em **Replay**, você deve ver agora 4 widgets sobre o
iRacing (em vez de apenas o HelloWidget de antes):

1. **Status** — canto superior esquerdo, painel pequeno com Track / Tick /
   Cars / State / Dots / Spotter (substitui o "iRadar — Fase 4")
2. **Radar** — abaixo do Status, círculo com triângulo verde no centro
   (você) e bolinhas coloridas representando carros próximos
3. **Relative** — direita do Status, lista textual `+0.34  #42  Driver`
   com seções AHEAD e BEHIND
4. **Spotter** — barras verticais nas laterais do overlay; em **replay
   ficam invisíveis** (iRacing não computa CarLeftRight em replay, como
   visto na Fase 1). Para validar é preciso entrar em sessão ao vivo (Test
   Drive offline serve).

---

## Checklist de validação

### Status widget
- [ ] Aparece no canto superior esquerdo
- [ ] Track mostra o nome correto do circuito
- [ ] Tick aumenta a cada frame
- [ ] Cars mostra o número total de carros
- [ ] State mostra `replay` em modo replay (ou `on-track` / `pit/garage`
      em sessão ao vivo)

### Radar widget
- [ ] Triângulo verde aparece no centro, apontando para cima
- [ ] Dois círculos concêntricos (linha cinza) representam os anéis de
      distância — o externo é 50m, o interno é 25m, label "25m" perto
      do interno
- [ ] Cruz cinza pelo centro (horizontal e vertical) para referência
- [ ] Bolinhas aparecem onde há carros próximos (até 50m)
- [ ] Cor das bolinhas:
  - cinza-esverdeado quando longe (Safe)
  - amarelo quando perto (Close, até ~20m)
  - vermelho quando muito perto (Danger, até ~8m)
- [ ] Quando o replay avança e carros mudam de posição relativa ao
      focused car, as bolinhas se movem suavemente

### Relative widget
- [ ] Aparece à direita do Status
- [ ] Seção `AHEAD` com até 3 carros com gap positivo (em vermelho)
- [ ] Separador
- [ ] Seção `BEHIND` com até 3 carros com gap negativo (em verde)
- [ ] Formato: `+0.34  #42  Driver Name` (gap com 2 decimais, número do
      carro, primeiros 16 chars do nome)
- [ ] Carros no pitlane aparecem em cinza com sufixo `(pit)`
- [ ] Quando não há ninguém em range: `(none in range)` em cinza

### Spotter widget (só valida em sessão ao vivo — Test Drive offline ok)
- [ ] **Em replay**: barras laterais sempre invisíveis (esperado)
- [ ] **Em Test Drive sozinho**: barras invisíveis (sem ninguém pra
      acusar)
- [ ] **Em AI Race offline** (Test Drive com AI): barra esquerda fica
      amarela quando um AI passa pela sua esquerda, vermelha se dois,
      mesma coisa pra direita

### Click-through (regressão da Fase 4)
- [ ] Cursor passando pelos widgets não muda comportamento
- [ ] Clicar sobre qualquer widget chega no iRacing (não rouba foco)

### Performance
- [ ] FPS do iRacing não cai notavelmente com o overlay ativo
- [ ] Sem stutter visível ao mover a câmera do replay

---

## Limitações conhecidas desta fase

- **Posições fixas**: você não consegue ainda mover ou redimensionar os
  widgets — é tudo posicionado em coordenadas fixas dentro do overlay
  window. Edit Mode (com hotkey `Ctrl+Alt+E`) é a próxima fase (Fase 6).

- **Lateral aproximado**: como o iRacing não expõe (x, y) absolutos para
  outros carros, a posição lateral dos dots no radar é uma aproximação
  baseada na hint `CarLeftRight`. Carros ao seu lado aparecem deslocados
  3.5m para a esquerda/direita do centro; cães em outras situações
  ficam alinhados ao eixo central. Em curvas isto pode parecer
  impreciso — Fase 7+ pode adicionar geometria de pista para refinar.

- **Em replay o spotter não funciona** — limitação do iRacing, não bug
  do app (já documentado em TESTING-FASE1.md).

---

## Como reportar de volta

- **Tudo OK**: `OK Fase 5` e sigo para Fase 6 (Edit Mode + configuração
  persistente)
- **Algo errado**: cola o que está vendo + as últimas linhas do
  `%LocalAppData%\iRadar\debug.log` que tiver mensagens `[overlay]`

Screenshots ajudam bastante se for problema visual (posicionamento,
cores, recorte da janela).
