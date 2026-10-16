class Bcpp < Formula
  desc "C(++) beautifier"
  homepage "https://invisible-island.net/bcpp/"
  url "https://invisible-mirror.net/archives/bcpp/bcpp-20250914.tgz"
  sha256 "8d2a0f6255243c7f422cbc8d9d65bb381cc6559879df967ba2838ac7d267be3f"
  license "MIT"

  livecheck do
    url "https://invisible-island.net/bcpp/CHANGES.html"
    regex(/id=.*?t(\d{6,8})["' >]/im)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "7464eb18d49d28fc597ea27cb69aafa2948ba8e9858bd476cb43b81449817780"
  end

  def install
    system "./configure", "--prefix=#{prefix}", "--mandir=#{man}"
    system "make", "install"
    etc.install "bcpp.cfg"
  end

  test do
    (testpath/"test.txt").write <<~EOS
          test
             test
      test
            test
    EOS
    system bin/"bcpp", "test.txt", "-fnc", "#{etc}/bcpp.cfg"
    assert_path_exists testpath/"test.txt.orig"
    assert_path_exists testpath/"test.txt"
  end
end
