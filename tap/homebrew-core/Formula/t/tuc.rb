class Tuc < Formula
  desc "Text manipulation and cutting tool"
  homepage "https://github.com/riquito/tuc"
  url "https://github.com/riquito/tuc/releases/download/v1.3.0/tuc-lite-windows-amd64.exe"
  sha256 "dd37a0943e6ddd12063818ddd20bfe72902918089b20d86e20c89a1f6ce3c54c"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/riquito/tuc/archive/refs/tags/v1.3.0.tar.gz"
  end

  def install
    bin.install "tuc-lite-windows-amd64.exe" => "tuc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tuc --version 2>&1")
  end
end
