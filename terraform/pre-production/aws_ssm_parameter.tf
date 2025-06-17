resource "aws_ssm_parameter" "financial_transaction_api_url" {
  name  = "/housing-finance/pre-production/financial-transaction-api-url"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}

resource "aws_ssm_parameter" "financial_transaction_api_token" {
  name  = "/housing-finance/pre-production/financial-transaction-api-token"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}

resource "aws_ssm_parameter" "asset_api_url" {
  name  = "/housing-tl/pre-production/asset-api-url"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}

resource "aws_ssm_parameter" "asset_api_token" {
  name  = "/housing-tl/pre-production/asset-api-token"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}

resource "aws_ssm_parameter" "process_api_url_v1" {
  name  = "/housing-tl/pre-production/process-api-url-v1"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}


resource "aws_ssm_parameter" "processes_api_token" {
  name  = "/housing-tl/pre-production/processes-api-token"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}

resource "aws_ssm_parameter" "contract_api_url" {
  name  = "/housing-tl/pre-production/contract-api-url"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}

resource "aws_ssm_parameter" "contract_api_token" {
  name  = "/housing-tl/pre-production/contract-api-token"
  type  = "String"
  value = "to_be_set_manually"

  lifecycle {
    ignore_changes = [
      value,
    ]
  }
}
