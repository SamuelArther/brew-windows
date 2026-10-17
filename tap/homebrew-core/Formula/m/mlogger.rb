class Mlogger < Formula
  desc "Log to syslog from the command-line"
  homepage "https://github.com/nbrownus/mlogger"
  url "https://github.com/nbrownus/mlogger/archive/refs/tags/v1.2.0.tar.gz"
  sha256 "141bb9af13a8f0e865c8509ac810c10be4e21f14db5256ef5c7a6731b490bf32"
  license "BSD-4-Clause-UC"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/mlogger-1.2.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "db9470b7eb2574f1fc17bffe3e99adef1ba742e9cadd297a9116358fa8207da4"
  end

  def install
    system "make"
    bin.install "mlogger"
  end

  test do
    system bin/"mlogger", "-i", "-d", "test"
  end
end
