class Mkp224o < Formula
  desc "Vanity address generator for tor onion v3 (ed25519) hidden services"
  homepage "https://github.com/cathugger/mkp224o"
  url "https://github.com/cathugger/mkp224o/releases/download/v1.7.0/mkp224o-1.7.0-src.tar.gz"
  sha256 "e38465ea893c6032ddfd7c133cbbf0de2eeaf1c428ca563fac5e85aeb609c929"
  license "CC0-1.0"
  head "https://github.com/cathugger/mkp224o.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8ed5cb9e56d867af787b58c308158dd2fd9652b09f9b6d4c8678b95770e60120"
  end

  depends_on "libsodium"

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make"
    bin.install "mkp224o"
  end

  test do
    assert_match "waiting for threads to finish...", shell_output("#{bin}/mkp224o -n 3 home 2>&1")
  end
end
