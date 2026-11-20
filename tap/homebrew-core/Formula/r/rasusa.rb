class Rasusa < Formula
  desc "Randomly subsample sequencing reads or alignments"
  homepage "https://doi.org/10.21105/joss.03941"
  url "https://github.com/mbhall88/rasusa/archive/refs/tags/5.1.1.tar.gz"
  sha256 "c9674624e2a24af5924b55709969e761e2b6abb43fe8cf45abfd40d693b65c26"
  license "MIT"
  head "https://github.com/mbhall88/rasusa.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1a75773e0ac6ea3ee23d0945f4afff8137ec446c56580566e885bbd6770093d7"
  end

  depends_on "rust" => :build

  uses_from_macos "llvm" => :build # for libclang

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    pkgshare.install "tests/cases"
  end

  test do
    cp_r pkgshare/"cases/.", testpath
    system bin/"rasusa", "reads", "-n", "5m", "-o", "out.fq", "file1.fq.gz"
    assert_path_exists "out.fq"
  end
end
