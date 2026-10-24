-- Technical fixture only; this is not production art or an approved palette.
local root = app.params.output or app.params.aseprite_mcp_workspace
assert(root and #root > 0, 'output folder is required')
local sprite = Sprite(16, 16, ColorMode.RGB)
local image = sprite.cels[1].image
local colors = {
  Color{r=44,g=40,b=62,a=255},
  Color{r=224,g=166,b=80,a=255},
  Color{r=112,g=174,b=120,a=255}
}
for y=2,13 do
  for x=2,13 do
    image:drawPixel(x,y,colors[1 + ((x+y) % 3)])
  end
end
sprite:saveAs(app.fs.joinPath(root,'a6-smoke.aseprite'))
sprite:saveCopyAs(app.fs.joinPath(root,'a6-smoke.png'))
sprite:close()
print('A6_SMOKE_OK 16x16')
