const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  transpileDependencies: true,
  publicPath: proocess.env.NODE_ENV ==='production' ? '/nbo-matching/' : '/',
  pluginOptions: {
    vuetify: {
      
    }
  }
})
