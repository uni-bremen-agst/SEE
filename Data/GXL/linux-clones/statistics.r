library(readr)
library(vioplot)
library(XML)

df <- read_delim("clones.stats.csv", ";", escape_double = FALSE, trim_ws = TRUE)
summary(df)

# normalize by z-score
#df$cloned_tokens = scale(df$cloned_tokens)
#df$tokens = scale(df$tokens)
#df$sloc = scale(df$sloc)
#df$clone_rate = scale(df$clone_rate)
#summary(df)

vioplot(df$tokens, df$sloc, df$clone_rate, names=c("tokens", "sloc", "clone rate"),
        col="gold")

hist(df$tokens)


f = function(x, m, s) {
  z = (x - m) / s
  
  if (z == 0.0) {
    result = 0.0
  } else if (z < 0.0) {
    result = x * abs(z)
  } else
  {
    result = x * z;
  }
  result
}

y = apply(df[,"tokens"], MARGIN = 1, FUN = f, m = mean(df$tokens), s = sd(df$tokens))

plot(x = df$tokens, y = (scale(df$tokens) + 1))
abline(v=mean(df$tokens))
abline(h=1)

sum(scale(df$tokens) + 1 < 0)

# number of data points above mean
sum(df$tokens > mean(df$tokens))

# percentage of data points above mean
sum(df$tokens > mean(df$tokens)) / length(df$tokens)

xmldoc <- xmlParse("arch-single-root.gxl")
rootNode <- xmlRoot(xmldoc)


#number.of.token = xpathApply(rootNode, "graph/node/attr[@name='Metric.Number_of_Tokens']/int/text()")
get.value = function(node) {
  node
} 

number.of.token = getNodeSet(rootNode, 
                             path = "graph/node/attr[@name='Metric.Number_of_Tokens']/int/text()",
                             fun = get.value)


gxl = data.frame(number.of.token)


data <- xmlSApply(rootNode,function(x) xmlSApply(x, xmlValue))
#gxl = data.frame(t(data),row.names=NULL)
