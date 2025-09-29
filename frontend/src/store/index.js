import { createStore } from 'vuex'
import auth from './auth'
import dataMatching from './dataMatching'
import results from './results'

export default createStore({
  state: {
    loading: false,
    error: null,
    successMessage: null
  },
  
  getters: {
    isLoading: state => state.loading,
    error: state => state.error,
    successMessage: state => state.successMessage,
    hasError: state => !!state.error,
    hasSuccessMessage: state => !!state.successMessage
  },
  
  mutations: {
    SET_LOADING(state, loading) {
      state.loading = loading
    },
    
    SET_ERROR(state, error) {
      state.error = error
      state.successMessage = null 
    },
    
  
    CLEAR_ERROR(state) {
      state.error = null 
    },
    
    SET_SUCCESS_MESSAGE(state, message) {
      state.successMessage = message
      state.error = null 
    },
    
    CLEAR_SUCCESS_MESSAGE(state) {
      state.successMessage = null
    },
    
    
    CLEAR_ALL_MESSAGES(state) {
      state.error = null
      state.successMessage = null
    }
  },
  
  actions: {
    setLoading({ commit }, loading) {
      commit('SET_LOADING', loading)
    },
    
    setError({ commit }, error) {
      commit('SET_ERROR', error)
    },
    
    
    clearError({ commit }) {
      commit('CLEAR_ERROR') 
    },
    
    setSuccessMessage({ commit }, message) {
      commit('SET_SUCCESS_MESSAGE', message)
    },
    
    clearSuccessMessage({ commit }) {
      commit('CLEAR_SUCCESS_MESSAGE')
    },
    
  
    clearAllMessages({ commit }) {
      commit('CLEAR_ALL_MESSAGES')
    },
    

    showTemporaryMessage({ dispatch }, { message, type = 'success', duration = 3000 }) {
      if (type === 'success') {
        dispatch('setSuccessMessage', message)
        setTimeout(() => {
          dispatch('clearSuccessMessage')
        }, duration)
      } else if (type === 'error') {
        dispatch('setError', message)
        setTimeout(() => {
          dispatch('clearError')
        }, duration)
      }
    }
  },
  
  modules: {
    auth,
    dataMatching,
    results

  }
})