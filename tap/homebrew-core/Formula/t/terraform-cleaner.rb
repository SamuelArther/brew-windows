class TerraformCleaner < Formula
  desc "Tiny utility which detects unused variables in your terraform modules"
  homepage "https://github.com/sylwit/terraform-cleaner"
  url "https://github.com/sylwit/terraform-cleaner/releases/download/v0.0.4/terraform-cleaner_0.0.4_windows_amd64.tar.gz"
  sha256 "216323c2197685cb5690b7d2c0867f6ba22b7ce5227d118a412d55fbcfde862d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sylwit/terraform-cleaner/archive/refs/tags/v0.0.4.tar.gz"
  end

  def install
    bin.install "terraform-cleaner.exe"
  end

  test do
    (testpath/"test.tf").write <<~HCL
      terraform {
        required_version = ">= 1.0"

        required_providers {
          aws = {
            source = "hashicorp/aws"
            version = "~> 5"
          }
        }
      }

      provider "aws" {
        region = var.aws_region
      }

      variable "aws_region" {
        type    = string
        default = "us-east-1"
      }

      variable "foo" {
        type    = string
        default = "unused"
      }
    HCL

    output = shell_output("#{bin}/terraform-cleaner --unused-only")
    assert_equal <<~EOS.chomp, output

       🚀 Module: .
       👉 1 variables found
      foo : 0

      1 modules processed
    EOS
  end
end
