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

locals {
  # These topic parameters have already been removed. A destroy plan must not read them.
  sns_topic_arn = "arn:aws:sns:eu-west-2:000000000000:removed"
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
                    "aws:SourceArn": "${local.sns_topic_arn}"
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
                    "aws:SourceArn": "${local.sns_topic_arn}"
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
                    "aws:SourceArn": "${local.sns_topic_arn}"
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
                    "aws:SourceArn": "${local.sns_topic_arn}"
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
                    "aws:SourceArn": "${local.sns_topic_arn}"
                }
            }
        }				
      ]
  }
  POLICY
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_person_sns" {
  topic_arn            = local.sns_topic_arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_tenure_sns" {
  topic_arn            = local.sns_topic_arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_accounts_sns" {
  topic_arn            = local.sns_topic_arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_asset_sns" {
  topic_arn            = local.sns_topic_arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "housing_search_listener_queue_subscribe_to_contracts_sns" {
  topic_arn            = local.sns_topic_arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.housing_search_listener_queue.arn
  raw_message_delivery = true
}

resource "aws_ssm_parameter" "housing_search_listeners_sqs_queue_arn" {
  name  = "/sqs-queue/pre-production/housing_search_listener_queue/arn"
  type  = "String"
  value = aws_sqs_queue.housing_search_listener_queue.arn
}
