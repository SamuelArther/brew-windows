class StripeMock < Formula
  desc "Mock HTTP server that responds like the real Stripe API"
  homepage "https://github.com/stripe/stripe-mock"
  url "https://github.com/stripe/stripe-mock/releases/download/v0.206.0/stripe-mock_0.206.0_windows_amd64.zip"
  sha256 "3ead9f1c24f8a60db6bf421d306c3c7e71d24393c0c769196fa6ccedbca93109"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stripe/stripe-mock/archive/refs/tags/v0.206.0.tar.gz"
  end

  def install
    bin.install "stripe-mock.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/stripe-mock --version 2>&1")
  end
end
