class Jql < Formula
  desc "JSON query language CLI tool"
  homepage "https://github.com/yamafaktory/jql"
  url "https://github.com/yamafaktory/jql/releases/download/jql-v9.0.3/jql-v9.0.3-x86_64-pc-windows-msvc.zip"
  sha256 "270816a8f06c96e6c4ecb32af379d3275991222f90648fa500f4572bbe68e2ba"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yamafaktory/jql/archive/refs/tags/jql-v9.0.3.tar.gz"
  end

  def install
    bin.install "jql.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jql --version 2>&1")
  end
end
