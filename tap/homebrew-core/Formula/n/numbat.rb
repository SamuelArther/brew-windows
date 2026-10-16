class Numbat < Formula
  desc "Statically typed programming language for scientific computations"
  homepage "https://numbat.dev/"
  url "https://github.com/sharkdp/numbat/releases/download/v1.24.0/numbat-v1.24.0-x86_64-pc-windows-msvc.zip"
  sha256 "450027111a5df0dd3ee4ce97a557c4847fc76b05851fccb9e26e875433084049"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sharkdp/numbat/archive/refs/tags/v1.24.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/numbat.exe", "numbat"
  end

  test do
    (testpath/"test.nbt").write <<~NBT
      print("pi = {pi}")
    NBT

    output = shell_output("#{bin}/numbat test.nbt")

    assert_equal "pi = 3.14159", output.chomp
  end
end
