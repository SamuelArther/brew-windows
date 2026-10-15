class Argc < Formula
  desc "Easily create and use cli based on bash script"
  homepage "https://github.com/sigoden/argc"
  url "https://github.com/sigoden/argc/releases/download/v1.24.0/argc-v1.24.0-x86_64-pc-windows-msvc.zip"
  sha256 "70f70b987cba68677db17c2012b6b7faf09a7374896b98c7ed4d039262f72914"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sigoden/argc/archive/refs/tags/v1.24.0.tar.gz"
  end

  def install
    bin.install "argc.exe"
  end

  test do
    system bin/"argc", "--argc-create", "build"
    assert_path_exists testpath/"Argcfile.sh"
    assert_match "build", shell_output("#{bin}/argc build")
  end
end
