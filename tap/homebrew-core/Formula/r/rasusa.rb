class Rasusa < Formula
  desc "Randomly subsample sequencing reads or alignments"
  homepage "https://doi.org/10.21105/joss.03941"
  url "https://github.com/mbhall88/rasusa/archive/refs/tags/5.1.0.tar.gz"
  sha256 "c58ee8cdb2e40c921fbef4b8ceffcb7999687b473152f942a6bda0089da6b256"
  license "MIT"
  head "https://github.com/mbhall88/rasusa.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7f9be1dd1da74ee5596c6f37afb9f74ad648f9f273ed375946a3b863d23c90dc"
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
