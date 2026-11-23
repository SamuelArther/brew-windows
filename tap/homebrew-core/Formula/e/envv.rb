class Envv < Formula
  desc "Shell-independent handling of environment variables"
  homepage "https://github.com/jakewendt/envv"
  url "https://github.com/jakewendt/envv/archive/refs/tags/v1.7.tar.gz"
  sha256 "1db05b46904e0cc4d777edf3ea14665f6157ade0567359e28663b5b00f6fa59a"
  license "MIT"

  # Fix missing header includes
  patch do
    url "https://github.com/jakewendt/envv/commit/0445a7e4ada9067b3817d1b03edfb91ba59a8962.patch?full_index=1"
    sha256 "7c778b4ab19f53feefaef65003249e9dbd09ac80fb4a7c0828bb357422eb8683"
    type :backport
    resolves "https://github.com/jakewendt/envv/pull/4"
  end

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6cdffe6f985d1ccae86a53a90ec615df0f13e48761febce9a9c56f75432065b2"
  end

  def install
    system "make"

    bin.install "envv"
    man1.install "envv.1"
  end

  test do
    ENV["mylist"] = "A:B:C"
    assert_equal "mylist=A:C; export mylist", shell_output("#{bin}/envv del mylist B").strip
    assert_equal "mylist=B:C; export mylist", shell_output("#{bin}/envv del mylist A").strip
    assert_equal "mylist=A:B; export mylist", shell_output("#{bin}/envv del mylist C").strip

    assert_empty shell_output("#{bin}/envv add mylist B").strip
    assert_equal "mylist=B:A:C; export mylist", shell_output("#{bin}/envv add mylist B 1").strip
    assert_equal "mylist=A:C:B; export mylist", shell_output("#{bin}/envv add mylist B 99").strip

    assert_equal "mylist=A:B:C:D; export mylist", shell_output("#{bin}/envv add mylist D").strip
    assert_equal "mylist=D:A:B:C; export mylist", shell_output("#{bin}/envv add mylist D 1").strip
    assert_equal "mylist=A:B:D:C; export mylist", shell_output("#{bin}/envv add mylist D 3").strip
  end
end
