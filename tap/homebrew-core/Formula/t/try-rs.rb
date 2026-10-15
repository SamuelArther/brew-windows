class TryRs < Formula
  desc "Temporary workspace manager for fast experimentation in the terminal"
  homepage "https://try-rs.org/"
  url "https://github.com/tassiovirginio/try-rs/releases/download/v1.7.11/try-rs-windows-x86_64.exe"
  sha256 "a59b135ad69a2239868e68976121062e12794a1242f651da95ccff9ac4971986"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tassiovirginio/try-rs/archive/refs/tags/v1.7.11.tar.gz"
  end

  def install
    bin.install "try-rs-windows-x86_64.exe" => "try-rs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/try-rs --version 2>&1")
  end
end
