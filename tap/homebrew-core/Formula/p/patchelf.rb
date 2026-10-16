class Patchelf < Formula
  desc "Modify dynamic ELF executables"
  homepage "https://github.com/NixOS/patchelf"
  url "https://github.com/NixOS/patchelf/releases/download/0.19.2/patchelf-win64-0.19.2.exe"
  sha256 "dadbe237f1b87ef5a0569bce763a55c3883278c3909db56958d7df9aa50b5c42"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/NixOS/patchelf/releases/download/0.19.2/patchelf-0.19.2.tar.bz2"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "patchelf-win64-0.19.2.exe" => "patchelf.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/patchelf --version 2>&1")
  end
end
