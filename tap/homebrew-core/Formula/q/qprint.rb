class Qprint < Formula
  desc "Encoder and decoder for quoted-printable encoding"
  homepage "https://www.fourmilab.ch/webtools/qprint/"
  url "https://www.fourmilab.ch/webtools/qprint/qprint-1.1.tar.gz"
  sha256 "ffa9ca1d51c871fb3b56a4bf0165418348cf080f01ff7e59cd04511b9665019c"
  license :public_domain

  livecheck do
    url :homepage
    regex(/href=.*?qprint[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/qprint-1.1"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b3b3f5587aab37e61c964df04f40a527c85d8b54caf45fdcc020930e727e3cc0"
  end

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make"
    bin.mkpath
    man1.mkpath
    system "make", "install"
  end

  test do
    msg = "test homebrew"
    encoded = pipe_output("#{bin}/qprint -e", msg)
    assert_equal msg, pipe_output("#{bin}/qprint -d", encoded)
  end
end
