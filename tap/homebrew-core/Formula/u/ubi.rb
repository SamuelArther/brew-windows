class Ubi < Formula
  desc "Universal Binary Installer"
  homepage "https://github.com/houseabsolute/ubi"
  url "https://github.com/houseabsolute/ubi/releases/download/v0.12.0/ubi-Windows-msvc-x86_64.zip"
  sha256 "5dd527177b8be61d5486ac69a268211a11dd4d3c137ce86bdccf29d8f657e9e2"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/houseabsolute/ubi/archive/refs/tags/v0.12.0.tar.gz"
  end

  def install
    bin.install "ubi.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ubi --version")

    system bin/"ubi", "--project", "houseabsolute/precious"
    system testpath/"bin/precious", "--version"
  end
end
