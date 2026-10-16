class Terratag < Formula
  desc "CLI to automate tagging for AWS, Azure & GCP resources in Terraform"
  homepage "https://www.terratag.io/"
  url "https://github.com/env0/terratag/releases/download/v0.8.0/terratag_0.8.0_windows_amd64.tar.gz"
  sha256 "c72f75ac18aab1b44a0eb61903c2e603c2c02c678b07e8841669f177bca280f1"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/env0/terratag/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "terratag.exe"
  end

  test do
    (testpath/"main.tf").write <<~HCL
      provider "aws" {
        region = "us-east-1"
      }

      resource "aws_instance" "example" {
        ami           = "ami-12345678"
        instance_type = "t2.micro"
      }
    HCL

    output = shell_output("#{bin}/terratag -dir #{testpath} " \
                          "-tags '{\"environment\":\"test\",\"owner\":\"brew\"}' -rename=false 2>&1", 1)

    assert_match "terraform init must run before running terratag", output
  end
end
