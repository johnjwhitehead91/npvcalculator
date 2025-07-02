export const environment = {
  production: true,
  apiUrl: 'https://production-api.com/api', // Production API URL
  apiEndpoints: {
    npv: {
      calculate: '/npv/calculate',
      status: '/npv/status',
      results: '/npv/results'
    }
  }
};
