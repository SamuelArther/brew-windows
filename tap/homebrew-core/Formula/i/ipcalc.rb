class Ipcalc < Formula
  desc "Calculate various network masks, etc. from a given IP address"
  homepage "https://jodies.de/ipcalc"
  url "https://github.com/kjokjo/ipcalc/archive/refs/tags/0.51.tar.gz"
  sha256 "a4dbfaeb7511b81830793ab9936bae9d7b1b561ad33e29106faaaf97ba1c117e"
  license "GPL-2.0-or-later"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "f561273bce78a8b73b820494e8d494f71ebf397d56976323efa2f43b87bc0e15"
  end

  def install
    bin.install "ipcalc"
  end

  test do
    system bin/"ipcalc", "--nobinary", "192.168.0.1/24"
  end
end
