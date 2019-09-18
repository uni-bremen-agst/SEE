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

#filename = "arch-single-root"
#gxlfile = paste(filename, ".gxl", sep="")
#csvfile = paste(filename, ".csv", sep="")

#filename = "net"
#gxlfile = paste(filename, ".gxl", sep="")
#csvfile = paste(filename, ".csv", sep="")

filename = "fs"
gxlfile = paste(filename, ".gxl", sep="")
csvfile = paste(filename, ".csv", sep="")

read.gxl = function(gxfile)
{
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
  gxl
}

gxl = read.gxl(gxlfile)

clone.statistics = function(gxl)
{
  cat("Number of tokens: mean=", mean(gxl$Number_Of_Tokens, na.rm = TRUE), "sd=", sd(gxl$Number_Of_Tokens, na.rm = TRUE))
  cat("Clone rate: mean=", mean(gxl$CloneRate, na.rm = TRUE), "sd=", sd(gxl$CloneRate, na.rm = TRUE))
  cat("LOC: mean=", mean(gxl$LOC, na.rm = TRUE), "sd=", sd(gxl$LOC, na.rm = TRUE))
}

clone.statistics(gxl)

# Yields a vector of randomized values having any desired correlation rho with Y.
# The optional X represents the regression function of the correlation. If X is 
# 1:length(y), a linear correlation is to be used. If X is omitted, the normal
# distribution is used.
# X any Y must have the same length.
#
# Taken from:
# https://stats.stackexchange.com/questions/15011/generate-a-random-variable-with-a-defined-correlation-to-an-existing-variables/313138#313138
complement <- function(y, rho, x) {
  stopifnot(length(x) == length(y))
  if (missing(x)) x <- rnorm(length(y)) # Optional: supply a default if `x` is not given
  y.perp <- residuals(lm(x ~ y))
  rho * sd(y.perp) * y + y.perp * sd(y) * sqrt(1 - rho^2)
}

# An example use of complement to experiment with.
try.complement = function() {
  y <- rnorm(50, sd=10)
  x <- 1:50 # Optional
  # draws six plots with six different values for rho ranging from -0.8 to 1.0
  rho <- seq(0, 1, length.out=6) * rep(c(-1,1), 3)
  X <- data.frame(z=as.vector(sapply(rho, function(rho) complement(y, rho, x))),
                  rho=ordered(rep(signif(rho, 2), each=length(y))),
                  y=rep(y, length(rho)))
  
  library(ggplot2)
  ggplot(X, aes(y,z, group=rho)) + 
    geom_smooth(method="lm", color="Black") + 
    geom_rug(sides="b") + 
    geom_point(aes(fill=rho), alpha=1/2, shape=21) +
    facet_wrap(~ rho, scales="free")
}

# metrics are randomly chosen from normal distribution using different scales
add.random.metrics = function(gxl)
{
  # all linkage names for the rows in gxl where all columns have values different from na
  Linkage.Name = gxl[complete.cases(gxl), ]$Linkname
  df = data.frame(Linkage.Name)
  df$Metric.Architecture_Violations = abs(rnorm(nrow(df), mean=4, sd=4))
  df$Metric.Clone = abs(rnorm(nrow(df), mean=20, sd=5))
  df$Metric.Dead_Code = abs(rnorm(nrow(df), mean=8, sd=2))
  df$Metric.Cycle = abs(rnorm(nrow(df), mean=0, sd=4))
  df$Metric.Metric = abs(rnorm(nrow(df), mean=100, sd=40))
  df$Metric.Style = abs(rnorm(nrow(df), mean=11100, sd=10000))
  df$Metric.Universal = abs(rnorm(nrow(df), mean=0, sd=0.1))
  df
}

# normalized randomized values of y correlated by function x with
# correlation factor rho. The normalized values are limited to the 
# range [0, 1.0].
random.correlated = function(y, rho, x) {
  result = complement(y, rho, x)
  minimum = min(result)
  if (minimum < 0) {
    result = result + abs(minimum)
  }
  result / max(result)
}

# metrics are randomly chosen but linearly correlated to number of tokens
add.random.correlated.metrics = function(gxl)
{
  gxl.without.na = gxl[complete.cases(gxl), ]
  # all linkage names for the rows in gxl where all columns have values different from na
  Linkage.Name = gxl.without.na$Linkname
  df = data.frame(Linkage.Name)
  df$Number_Of_Tokens = gxl.without.na$Number_Of_Tokens
  
  x = 1:nrow(df) # we want a linear correlation
  df$Metric.Architecture_Violations = random.correlated(df$Number_Of_Tokens, 0.8, x)
  df$Metric.Clone                   = random.correlated(df$Number_Of_Tokens, 0.7, x)
  df$Metric.Dead_Code               = random.correlated(df$Number_Of_Tokens, 0.5, x)
  df$Metric.Cycle                   = random.correlated(df$Number_Of_Tokens, 0.9, x)
  df$Metric.Metric                  = random.correlated(df$Number_Of_Tokens, 0.6, x)
  df$Metric.Style                   = random.correlated(df$Number_Of_Tokens, 0.4, x)
  df$Metric.Universal               = random.correlated(df$Number_Of_Tokens, 0.85, x)
  df
}

#metrics = add.random.metrics(gxl)
metrics = add.random.correlated.metrics(gxl)

#plot(metrics$Number_Of_Tokens, metrics$Metric.Architecture_Violations)

write.table(metrics, csvfile, quote=FALSE, sep=";", row.names=FALSE, col.names=TRUE, dec=".", fileEncoding = "UTF-8")

