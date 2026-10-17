class Bfs < Formula
  desc "Breadth-first version of find"
  homepage "https://tavianator.com/projects/bfs.html"
  url "https://github.com/tavianator/bfs/archive/refs/tags/4.1.4.tar.gz"
  sha256 "0cac6849efb8a9447268fb273de3fab38f8460adb26a1770934e3f325fab8f5d"
  license "0BSD"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5f3872d9fc4d68d41708f59be92f9dee5c73ef0a3c80a8e0385b853e45c0aca7"
  end

  depends_on "pkgconf" => :build
  depends_on "oniguruma"

  on_macos do
    depends_on "llvm" => :build if DevelopmentTools.clang_build_version <= 1300
  end

  on_linux do
    depends_on "acl"
    depends_on "libcap"
    depends_on "liburing"
  end

  fails_with :clang do
    build 1300
  end

  def install
    system "./configure", "--enable-release"
    system "make"
    system "make", "install", "DEST_PREFIX=#{prefix}", "DEST_MANDIR=#{man}"
    bash_completion.install share/"bash-completion/completions/bfs"
  end

  test do
    touch "foo_file"
    touch "test_file"
    assert_equal "./test_file", shell_output("#{bin}/bfs -name 'test*' -regextype emacs -depth 1").chomp
  end
end
