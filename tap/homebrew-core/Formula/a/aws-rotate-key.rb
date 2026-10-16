class AwsRotateKey < Formula
  desc "Easily rotate your AWS access key"
  homepage "https://github.com/stefansundin/aws-rotate-key"
  url "https://github.com/stefansundin/aws-rotate-key/releases/download/v1.2.0/aws-rotate-key-1.2.0-windows_amd64.zip"
  sha256 "b1370d7b5e96fd011c203b1e44b998b66630d4413f300f1de8fd35b06af77a96"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stefansundin/aws-rotate-key/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    bin.install "aws-rotate-key.exe"
  end

  test do
    assert_path_exists bin/"aws-rotate-key.exe"
  end
end
