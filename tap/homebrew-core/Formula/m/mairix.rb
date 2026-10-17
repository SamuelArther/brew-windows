class Mairix < Formula
  desc "Email index and search tool"
  homepage "https://github.com/vandry/mairix"
  url "https://github.com/vandry/mairix/releases/download/0.24/mairix-0.24.tar.gz"
  sha256 "a0702e079c768b6fbe25687ebcbabe7965eb493d269a105998c7c1c2caef4a57"
  license "GPL-2.0-only"
  head "https://github.com/vandry/mairix.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9cf174aa306b59a4e795af14ac904a2640b752b0d7df83f40b46fbb16819d9e2"
  end

  uses_from_macos "bison" => :build
  uses_from_macos "flex" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  def install
    ENV.deparallelize
    system "./configure", "--prefix=#{prefix}", "--mandir=#{man}"
    system "make"
    system "make", "install"
  end

  test do
    system bin/"mairix", "--version"
  end
end
