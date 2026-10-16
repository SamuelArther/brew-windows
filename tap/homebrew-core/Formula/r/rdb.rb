class Rdb < Formula
  desc "Redis RDB parser"
  homepage "https://github.com/HDT3213/rdb/"
  url "https://github.com/HDT3213/rdb/archive/refs/tags/v1.3.2.tar.gz"
  sha256 "d5e29babc24a21e9270a651c4fc4382bff8c62b5624ba754cc9f81cb8bb2073e"
  license "Apache-2.0"
  head "https://github.com/HDT3213/rdb.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "98ef5e8e4ab7b485a053e57499ae294fd8a8fad84cba80004c6684c9ff96c340"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    pkgshare.install "cases"
  end

  test do
    cp_r pkgshare/"cases", testpath
    system bin/"rdb", "-c", "memory", "-o", testpath/"mem1.csv", testpath/"cases/memory.rdb"
    assert_match "0,hash,hash,131,131B,2,ziplist,", (testpath/"mem1.csv").read
  end
end
