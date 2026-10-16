class Reop < Formula
  desc "Encrypted keypair management"
  homepage "https://web.archive.org/web/20251224210131/https://flak.tedunangst.com/post/reop"
  url "https://pkg.freebsd.org/ports-distfiles/reop-2.1.1.tgz"
  mirror "https://ftp.openbsd.org/pub/OpenBSD/distfiles/reop-2.1.1.tgz"
  sha256 "fa8ae058c51efec5bde39fab15b4275e6394d9ab1dd2190ffdba3cf9983fdcac"
  license "ISC"
  revision 1

  deprecate! date: "2026-06-27", because: :unmaintained
  disable! date: "2026-09-27", because: :unmaintained

  depends_on "libsodium"

  def install
    # Temporary Homebrew-specific work around for linker flag ordering problem in Ubuntu 16.04.
    # Remove after migration to 18.04.
    inreplace "GNUmakefile", "${LDFLAGS} ${OBJS}", "${OBJS} ${LDFLAGS}"

    system "make", "-f", "GNUmakefile"
    bin.install "reop"
    man1.install "reop.1"
  end

  test do
    (testpath/"pubkey").write <<~EOS
      -----BEGIN REOP PUBLIC KEY-----
      ident:root
      RWRDUxZNDeX4wcynGeCr9Bro6Ic7s1iqi1XHYacEaHoy+7jOP+ZE0yxR+2sfaph2MW15e8eUZvvI
      +lxZaqFZR6Kc4uVJnvirIK97IQ==
      -----END REOP PUBLIC KEY-----
    EOS

    (testpath/"msg").write <<~EOS
      testing one two three four
    EOS

    (testpath/"sig").write <<~EOS
      -----BEGIN REOP SIGNATURE-----
      ident:root
      RWQWTQ3l+MHMpx8RO/+BX/xxHn0PiSneiJ1Au2GurAmx4L942nZFBRDOVw2xLzvp/RggTVTZ46k+
      GLVjoS6fSuLneCfaoRlYHgk=
      -----END REOP SIGNATURE-----
    EOS

    system bin/"reop", "-V", "-x", "sig", "-p", "pubkey", "-m", "msg"
  end
end
