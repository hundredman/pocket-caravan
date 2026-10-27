-- Editable native water ripple loop; no interpolation or generated-frame resizing.
local folder=assert(app.params.folder)
local s=Sprite(16,16,ColorMode.RGB)
s.layers[1].name='water ripple'
for f=0,3 do
 if f>0 then s:newEmptyFrame() end
 local im=Image{fromFile=app.fs.joinPath(folder,'pieces/ripple_'..f..'.png')}
 if f==0 then s.cels[1].image=im else s:newCel(s.layers[1],f+1,im,Point(0,0)) end
 s.frames[f+1].duration=.18
end
local t=s:newTag(1,4);t.name='ripple'
s:saveAs(app.fs.joinPath(folder,'ripple.aseprite'))
s:close()
