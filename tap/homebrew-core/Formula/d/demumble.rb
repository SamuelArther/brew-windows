class Demumble < Formula
  desc "More powerful symbol demangler (a la c++filt)"
  homepage "https://github.com/nico/demumble"
  url "https://github.com/nico/demumble/releases/download/v1.3.0/demumble-win.zip"
  sha256 "bb86348938337d02df4f83ea6ba2e52587711822e8b1d623873bf241d239e8cb"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nico/demumble/archive/refs/tags/v1.3.0.tar.gz"
  end

  def install
    bin.install "demumble.exe"
  end

  test do
    mangled = "__imp_?FLAGS_logtostderr@fLB@@3_NA"
    demangled = "__imp_bool fLB::FLAGS_logtostderr"
    assert_equal demangled, pipe_output(bin/"demumble", mangled)
  end
end
