class Xk6 < Formula
  desc "Build k6 with extensions"
  homepage "https://k6.io"
  url "https://github.com/grafana/xk6/releases/download/v1.4.14/xk6_1.4.14_windows_amd64.zip"
  sha256 "f82274c276b18dddd3c017d6d1b5a8e55eed9c704a64395a38056a503dbf69ae"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/grafana/xk6/releases/download/v1.4.14/xk6_1.4.14_source.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "xk6.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xk6 --version 2>&1")
  end
end
