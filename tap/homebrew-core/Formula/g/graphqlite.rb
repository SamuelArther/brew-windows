class Graphqlite < Formula
  desc "SQLite graph database extension"
  homepage "https://colliery-io.github.io/graphqlite/"
  url "https://github.com/colliery-io/graphqlite/releases/download/v0.10.0/gqlite-windows-x86_64.exe"
  sha256 "b10b1c75b96d91436d182ed87d550df983c7f0ef827bca0c3693e33fab18fe73"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/colliery-io/graphqlite/archive/refs/tags/v0.10.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "gqlite-windows-x86_64.exe" => "graphqlite.exe"
  end

  test do
    system bin/"graphqlite", "--help"
  end
end
