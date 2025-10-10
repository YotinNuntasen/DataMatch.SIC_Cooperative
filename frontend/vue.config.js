const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  transpileDependencies: true,
  publicPath: '/',
  pluginOptions: {
    vuetify: {
      
    }
  }
})


// process.env.NODE_ENV ==='production' ? '/nbo-matching/' : '/',