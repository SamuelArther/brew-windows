class LiefPatchelf < Formula
  desc "Robust, modern reimplementation of patchelf based on the LIEF"
  homepage "https://lief.re/doc/latest/tools/lief-patchelf/index.html"
  url "https://github.com/lief-project/LIEF/archive/refs/tags/1.0.0.tar.gz"
  sha256 "2cf412695ff739d82e129db441e5c2025f3bb4873a3d3a1d3dd4cf300b682abd"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "18c6e83ee992ce38f9cb77b44e1496899fc16bf9bb6cd68d386ff63a36ebeddc"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "tools/lief-patchelf")
    system bin/"lief-patchelf", "--generate-manpage", man1.mkpath/"lief-patchelf.1"
    generate_completions_from_executable(bin/"lief-patchelf", "--generate", shells: [:bash, :zsh, :fish, :pwsh])
  end

  test do
    cp test_fixtures("elf/hello"), testpath
    assert_equal "/lib64/ld-linux-x86-64.so.2\n", shell_output("#{bin}/lief-patchelf --print-interpreter hello")
    assert_equal "libc.so.6\n", shell_output("#{bin}/lief-patchelf --print-needed hello")
    assert_empty shell_output("#{bin}/lief-patchelf --print-rpath hello")
    assert_empty shell_output("#{bin}/lief-patchelf --set-rpath /usr/local/lib hello")
    assert_equal "/usr/local/lib\n", shell_output("#{bin}/lief-patchelf --print-rpath hello")
  end
end
