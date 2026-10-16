class Fileql < Formula
  desc "Run SQL-like query on local files instead of database files using the GitQL SDK"
  homepage "https://github.com/AmrDeveloper/FileQL"
  url "https://github.com/AmrDeveloper/FileQL/releases/download/0.10.0/fileql-x86_64-windows.exe"
  sha256 "fa4b8e519eb8adcc01fcf6aff80076c101820e758422dc67c595094e6da48878"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/AmrDeveloper/FileQL/archive/refs/tags/0.10.0.tar.gz"
  end

  def install
    bin.install "fileql-x86_64-windows.exe" => "fileql.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fileql --version 2>&1")
  end
end
