class Wget < Formula
  desc "Internet file retriever"
  homepage "https://www.gnu.org/software/wget/"
  url "https://repo.msys2.org/msys/x86_64/wget-1.25.0-4-x86_64.pkg.tar.zst"
  sha256 "533bd9beb98fe40789558de0343aa29361709876af6a3b9eb2e83f88a18645d3"
  license "GPL-3.0-or-later"
  revision 2

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "ca-certificates" do
    url "https://repo.msys2.org/msys/x86_64/ca-certificates-20260816-1-any.pkg.tar.zst"
    sha256 "b7b2344aa87022919d6c4fd2bd9410e6490835c9a2d43092c1284186bae8c5d0"
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "db" do
    url "https://repo.msys2.org/msys/x86_64/db-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "41bc94acad3b53eaa92227ec34412305f648848ffbc963bcad88274517e13e8d"
  end

  resource "findutils" do
    url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
    sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  end

  resource "gawk" do
    url "https://repo.msys2.org/msys/x86_64/gawk-5.4.1-1-x86_64.pkg.tar.zst"
    sha256 "4b1322cb6c83d61fde5100e229e3cabc5b7d01fc0d0d8bc311edc08df91916be"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gdbm" do
    url "https://repo.msys2.org/msys/x86_64/gdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "1f8e949be2bbe488c5644132db78ecda288c9945922e7ed2447384734e2adba2"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "grep" do
    url "https://repo.msys2.org/msys/x86_64/grep-1~3.0-7-x86_64.pkg.tar.zst"
    sha256 "2909b3bbef8d33d66980339fb7bd2d3d9145bae0f04191e6671df39868aea427"
  end

  resource "gzip" do
    url "https://repo.msys2.org/msys/x86_64/gzip-1.15-1-x86_64.pkg.tar.zst"
    sha256 "3f967c87cf041391d0d43819c91174742917ccc2d1bb9419050df29ef22f4bfd"
  end

  resource "info" do
    url "https://repo.msys2.org/msys/x86_64/info-7.2-3-x86_64.pkg.tar.zst"
    sha256 "69242603357aa7011356f7d15a2017ad086e5682c8577f13520943c94bd2b03e"
  end

  resource "less" do
    url "https://repo.msys2.org/msys/x86_64/less-710-1-x86_64.pkg.tar.zst"
    sha256 "894a643ecd01861490e5767873d67b55cd5a2faf2c77b0b24a62dbba7bd038fd"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libgdbm" do
    url "https://repo.msys2.org/msys/x86_64/libgdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "bb4b08b720dd3194db010589812e9c63bec3d29b32545e7cbd07b6443e51c5ed"
  end

  resource "libgnutls" do
    url "https://repo.msys2.org/msys/x86_64/libgnutls-3.8.13-2-x86_64.pkg.tar.zst"
    sha256 "144a34bda1456dfbc0ca83ce8ed1ce49e89d750987ef096ab4083676dbcb373e"
  end

  resource "libhogweed" do
    url "https://repo.msys2.org/msys/x86_64/libhogweed-4.0-1-x86_64.pkg.tar.zst"
    sha256 "1302b7e86c5c54c93bd431685ccefa89df5f1b01257c68e24f2609df953cfdc2"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libidn2" do
    url "https://repo.msys2.org/msys/x86_64/libidn2-2.3.8-1-x86_64.pkg.tar.zst"
    sha256 "55e0de96b893e9f67f14a4283d411fc2a45d7b59333bc16e2a9b16e5bfc9282f"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libnettle" do
    url "https://repo.msys2.org/msys/x86_64/libnettle-4.0-1-x86_64.pkg.tar.zst"
    sha256 "aa2a398eb5af88247f29d5de683c6d1d7411733c07cc5e6c62803e1e77337cf7"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libp11-kit" do
    url "https://repo.msys2.org/msys/x86_64/libp11-kit-0.26.5-1-x86_64.pkg.tar.zst"
    sha256 "62707348cea67bf4ee009dbf25548c84e9f06a76b688590bd14e76321d58e125"
  end

  resource "libpcre" do
    url "https://repo.msys2.org/msys/x86_64/libpcre-8.45-5-x86_64.pkg.tar.zst"
    sha256 "b4dc2eb7795cbacaa683101d720fa67f536262497a1d89934a58f5dab8d17d17"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
  end

  resource "libpsl" do
    url "https://repo.msys2.org/msys/x86_64/libpsl-0.23.2-1-x86_64.pkg.tar.zst"
    sha256 "749014f86a1ae5f5a5adccec518e0296a03a838db9874aa9803dffaa8d30abc8"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libtasn1" do
    url "https://repo.msys2.org/msys/x86_64/libtasn1-4.21.0-1-x86_64.pkg.tar.zst"
    sha256 "14f15c4b2b6ebeb6bfcf060cab27a32cd8967f70b0e68cc14670062cce546f04"
  end

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
  end

  resource "libutil-linux" do
    url "https://repo.msys2.org/msys/x86_64/libutil-linux-2.40.2-2-x86_64.pkg.tar.zst"
    sha256 "148ec8e668cb0a30ea0c4e2687e16e0953d6628305598f4c3225bedf2564192d"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "m4" do
    url "https://repo.msys2.org/msys/x86_64/m4-1.4.21-1-x86_64.pkg.tar.zst"
    sha256 "b561e8871c7599502d9fae7196ee965b3fed3d7af09154eb8d2f5e4d9e5e5106"
  end

  resource "mpfr" do
    url "https://repo.msys2.org/msys/x86_64/mpfr-4.2.2-1-x86_64.pkg.tar.zst"
    sha256 "8f1b6c17c53e00494a488e03dfdf8fc742027ed03a5bd7b1b3a6a33f595a7498"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  resource "p11-kit" do
    url "https://repo.msys2.org/msys/x86_64/p11-kit-0.26.5-1-x86_64.pkg.tar.zst"
    sha256 "db341cc3e91c68f5de25d9a2eda3beb752ec8ac74a89f001f60453c817ce5f51"
  end

  resource "perl" do
    url "https://repo.msys2.org/msys/x86_64/perl-5.42.3-3-x86_64.pkg.tar.zst"
    sha256 "6a4dbc38cdcc53d16239904b5c87d2277cffc49ed92c6a7796f8bf6b49517d5b"
  end

  resource "sed" do
    url "https://repo.msys2.org/msys/x86_64/sed-4.9-1-x86_64.pkg.tar.zst"
    sha256 "3748af28f69e946ec5a42e6670c9bbf6da7352dc93baaa537f69e99c5483b9fc"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    # pacman normally runs update-ca-trust after installing ca-certificates; without it the
    # CA bundle wget reads (/usr/ssl/certs/ca-bundle.crt) is empty and every HTTPS fetch fails.
    system libexec/"usr/bin/p11-kit.exe", "extract", "--format=pem-bundle", "--filter=ca-anchors",
           "--overwrite", "--comment", "--purpose", "server-auth", "/usr/ssl/certs/ca-bundle.crt"
    # p11-kit creates it read-only, which would stop `brew uninstall` from deleting it.
    system libexec/"usr/bin/chmod.exe", "644", "/usr/ssl/certs/ca-bundle.crt"
    msys2_bin "usr/bin/wget.exe", "wget"
  end

  test do
    system bin/"wget", "-O", testpath/"google.html", "https://google.com"
    assert_path_exists testpath/"google.html"

    # Verify PSL support is built in via libpsl
    assert_match "+psl", shell_output("#{bin}/wget --version")
  end
end
