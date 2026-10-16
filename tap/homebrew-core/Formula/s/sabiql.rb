class Sabiql < Formula
  desc "Fast, safe-by-design, driverless, Vim-first DB TUI with ER diagrams"
  homepage "https://github.com/riii111/sabiql"
  url "https://github.com/riii111/sabiql/releases/download/v3.0.1/sabiql-x86_64-pc-windows-msvc.zip"
  sha256 "8eb60524b386fc770912109be12f4ecd68705a8eba31ac5e630dc4b7cd142873"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/riii111/sabiql/archive/refs/tags/v3.0.1.tar.gz"
  end

  def install
    bin.install "sabiql.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sabiql --version 2>&1")
  end
end
