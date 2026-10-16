class Gitql < Formula
  desc "Git query language"
  homepage "https://github.com/filhodanuvem/gitql"
  url "https://github.com/filhodanuvem/gitql/releases/download/v2.3.1/gitql_2.3.1_Windows_x86_64.tar.gz"
  sha256 "765394d0297e75c03ec283717f96c0b754a4c6d1294189bc9c51f902c8915565"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/filhodanuvem/gitql/archive/refs/tags/v2.3.1.tar.gz"
  end

  conflicts_with "gql", because: "both install `gitql` binaries"

  def install
    bin.install "gitql.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitql --version 2>&1")
  end
end
