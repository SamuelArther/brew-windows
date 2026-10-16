class Fsql < Formula
  desc "Search through your filesystem with SQL-esque queries"
  homepage "https://github.com/kashav/fsql"
  url "https://github.com/kashav/fsql/releases/download/v0.5.2/fsql-0.5.2-fsql-windows-amd64.tar.gz"
  sha256 "4ae80ffe57e55f1a41759f9946b7448cfff74ebb74437a70c3a49b1e3d932054"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kashav/fsql/archive/refs/tags/v0.5.2.tar.gz"
  end

  def install
    bin.install "fsql.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fsql --version 2>&1")
  end
end
