library(xml2)
library(purrr)

gxl = '<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE gxl SYSTEM "http://www.gupro.de/GXL/gxl-1.0.dtd">
<gxl xmlns:xlink="http://www.w3.org/1999/xlink">
<graph id="arch" edgeids="true">
<node id="N1">
<type xlink:href="File"/>
<attr name="Metric.Number_of_Tokens">
<int>695</int>
</attr>
<attr name="Metric.LOC">
<int>132</int>
</attr>
<attr name="Metric.Clone_Rate">
<float>0.392806</float>
</attr>
<attr name="Source.Name">
<string>bootp.c</string>
</attr>
<attr name="Linkage.Name">
<string>arch/alpha/boot/bootp.c</string>
</attr>
<attr name="Source.File">
<string>bootp.c</string>
</attr>
<attr name="Source.Path">
<string>arch/alpha/boot/bootp.c</string>
</attr>
</node>
    <node id="N5">
      <type xlink:href="File"/>
      <attr name="Metric.Number_of_Tokens">
        <int>699</int>
      </attr>
      <attr name="Metric.LOC">
        <int>139</int>
      </attr>
      <attr name="Metric.Clone_Rate">
        <float>0.390558</float>
      </attr>
      <attr name="Source.Name">
        <string>main.c</string>
      </attr>
      <attr name="Linkage.Name">
        <string>arch/alpha/boot/main.c</string>
      </attr>
      <attr name="Source.File">
        <string>main.c</string>
      </attr>
      <attr name="Source.Path">
        <string>arch/alpha/boot/main.c</string>
      </attr>
    </node>
</graph>
</gxl>'

gxlfile = "arch-single-root.gxl"
doc <- read_xml(gxlfile)
gxl = xml_find_all(doc, "graph/node") %>% 
  map_df(function(x) {
    list(
      Node=xml_attr(x, "id"), #,
      Linkname=xml_find_first(x, ".//attr[@name='Linkage.Name']/string") %>%  xml_text(),
      Number_Of_Tokens=xml_find_first(x, ".//attr[@name='Metric.Number_of_Tokens']/int") %>% xml_text() %>% strtoi(),
      LOC=xml_find_first(x, ".//attr[@name='Metric.LOC']/int") %>% xml_text() %>% strtoi(),
      CloneRate=xml_find_first(x, ".//attr[@name='Metric.Clone_Rate']/float") %>% xml_text() %>% as.numeric()
    )
  })

cat("Number of tokens: mean=", mean(gxl$Number_Of_Tokens, na.rm = TRUE), "sd=", sd(gxl$Number_Of_Tokens, na.rm = TRUE))
cat("Clone rate: mean=", mean(gxl$CloneRate, na.rm = TRUE), "sd=", sd(gxl$CloneRate, na.rm = TRUE))
cat("LOC: mean=", mean(gxl$LOC, na.rm = TRUE), "sd=", sd(gxl$LOC, na.rm = TRUE))
