class Sqsmover < Formula
  desc "AWS SQS Message mover"
  homepage "https://github.com/smaskalik/sqsmover"
  url "https://github.com/smaskalik/sqsmover/releases/download/v0.4.0/sqsmover_Windows_x86_64.zip"
  sha256 "06600f7bf220229cf76e20e3a090a781cbf65104623d9bf202e8fe29853f8d24"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/smaskalik/sqsmover/archive/refs/tags/v0.4.0.tar.gz"
  end

  def install
    bin.install "sqsmover.exe"
  end

  test do
    ENV["AWS_REGION"] = "us-east-1"
    assert_match "Failed to resolve source queue.",
      shell_output("#{bin}/sqsmover --source test-dlq --destination test --profile test 2>&1")

    assert_match version.to_s, shell_output("#{bin}/sqsmover --version 2>&1")
  end
end
