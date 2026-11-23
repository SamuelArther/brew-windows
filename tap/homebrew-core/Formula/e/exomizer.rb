class Exomizer < Formula
  desc "File compressor optimized for decompression in 8-bit environments"
  homepage "https://bitbucket.org/magli143/exomizer/"
  url "https://bitbucket.org/magli143/exomizer/get/3.1.2.tar.gz"
  sha256 "01713f2ee39efc86cbe54bb6011613e097efc3bc532cdc640809e7589cd9c043"
  license all_of: [
    "Zlib",
    "GPL-3.0-or-later" => { with: "Bison-exception-2.2" },
  ]
  head "https://bitbucket.org/magli143/exomizer.git", branch: "master"

  livecheck do
    url :head
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "aa888eac3b4fcfd86b90ce1c067b4911d08e1666fb99f5202b7a0d8d76445bea"
  end

  deny_network_access!

  def install
    cd "src" do
      system "make"
      bin.install %w[exobasic exomizer]
    end
  end

  test do
    (testpath/"test.txt").write("Hello World! Hello World! Hello World! Hello World!\n")
    system bin/"exomizer", "raw", "test.txt", "-o", "compressed.exo"
    system bin/"exomizer", "raw", "-d", "compressed.exo", "-o", "expanded.txt"
    assert_match "iHelo", File.binread("compressed.exo")
    assert_match File.read("test.txt"), File.read("expanded.txt")
  end
end
