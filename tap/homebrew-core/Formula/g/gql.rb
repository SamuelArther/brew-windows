class Gql < Formula
  desc "Git Query language is a SQL like language to perform queries on .git files"
  homepage "https://amrdeveloper.github.io/GQL/"
  url "https://github.com/AmrDeveloper/GQL/releases/download/0.43.0/gql-x86_64-windows.exe"
  sha256 "54a6fcfe38233b5ff1efa69fe796e6638da20a70368d4f79337519c0bc699708"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/AmrDeveloper/GQL/archive/refs/tags/0.43.0.tar.gz"
  end

  conflicts_with "gitql", because: "both install `gitql` binaries"

  def install
    bin.install "gql-x86_64-windows.exe" => "gitql.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitql --version 2>&1")
  end
end
