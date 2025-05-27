terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.0"
    }
  }
}

provider "aws" {
  region = "eu-west-2"
}

data "aws_ssm_parameter" "person_sns_topic_arn" {
  name = "/sns-topic/pre-production/person/arn"
}

data "aws_ssm_parameter" "tenure_sns_topic_arn" {
  name = "/sns-topic/pre-production/tenure/arn"
}

data "aws_ssm_parameter" "accounts_sns_topic_arn" {
  name = "/sns-topic/pre-production/accounts/arn"
}

data "aws_ssm_parameter" "asset_sns_topic_arn" {
  name = "/sns-topic/pre-production/asset/arn"
}

data "aws_ssm_parameter" "contracts_sns_topic_arn" {
  name = "/sns-topic/pre-production/contracts/arn"
}

terraform {
  backend "s3" {
    bucket         = "housing-pre-production-terraform-state"
    encrypt        = true
    region         = "eu-west-2"
    key            = "services/housing-search-listener/state"
    dynamodb_table = "housing-pre-production-terraform-state-lock"
  }
}

resource "aws_sqs_queue" "housing_search_dead_letter_queue" {
  name                              = "housingsearchdeadletterqueue.fifo"
  fifo_queue                        = true
  content_based_deduplication       = true
  kms_master_key_id                 = "alias/housing-pre-production-cmk"
  kms_data_key_reuse_period_seconds = 300
}

resource "aws_sqs_queue" "housing_search_listener_queue" {
  name                              = "housingsearchqueue.fifo"
  fifo_queue                        = true
  content_based_deduplication       = true
  kms_master_key_id                 = "alias/housing-pre-production-cmk"
  kms_data_key_reuse_period_seconds = 300
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.housing_search_dead_letter_queue.arn,
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue_policy" "housing_search_listener_queue_policy" {
  queue_url = aws_sqs_queue.housing_search_listener_queue.id
  policy    = <<POLICY
  {
        "Version": "2012-10-17",
        "Id": "sqspolicy",
        "Statement": [
        {
            "Sid": "First",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "sqs:SendMessage",
            "Resource": "${aws_sqs_queue.housing_search_listener_queue.arn}",
            "Condition": {
                "ArnEquals": {
                    "aws:SourceArn": "${data.aws_ssm_parameter.person_sns_topic_arn.value}"
                }
            }
        },
        {
            "Sid": "Second",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "sqs:SendMessage",
            "Resource": "${aws_sqs_queue.housing_search_listener_queue.arn}",
            "Condition": {
                "ArnEquals": {
                    "aws:SourceArn": "${data.aws_ssm_parameter.tenure_sns_topic_arn.value}"
                }
            }
        },
        {
            "Sid": "Third",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "sqs:SendMessage",
            "Resource": "${aws_sqs_queue.housing_search_listener_queue.arn}",
            "Condition": {
                "ArnEquals": {
                    "aws:SourceArn": "${data.aws_ssm_parameter.accounts_sns_topic_arn.value}"
                }
            }
        },
        {
            "Sid": "Fourth",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "sqs:SendMessage",
            "Resource": "${aws_sqs_queue.housing_search_listener_queue.arn}",
            "Condition": {
                "ArnEquals": {
                    "aws:SourceArn": "${data.aws_ssm_parameter.asset_sns_topic_arn.value}"
                }
            }
        },
        {
            "Sid": "Seventh",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "sqs:SendMessage",
            "Resource": "${aws_sqs_queue.housing_search_listener_queue.arn}",
            "Condition": {
                "ArnEquals": {
                    "aws:SourceArn": "${data.aws_ssm_parameter.contracts_sns_topic_arn.value}"
                }
            }
        }				
      ]
  }
  POLICY
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_person_sns" {
  topic_arn            = data.aws_ssm_parameter.person_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_tenure_sns" {
  topic_arn            = data.aws_ssm_parameter.tenure_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_accounts_sns" {
  topic_arn            = data.aws_ssm_parameter.accounts_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_asset_sns" {
  topic_arn            = data.aws_ssm_parameter.asset_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_contracts_sns" {
  topic_arn            = data.aws_ssm_parameter.contracts_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_ssm_parameter" "housing_search_listeners_sqs_queue_arn" {
  name  = "/sqs-queue/pre-production/housing_search_listener_queue/arn"
  type  = "String"
  value = aws_sqs_queue.housing_search_listener_queue.arn
}
