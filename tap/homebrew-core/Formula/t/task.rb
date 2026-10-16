class Task < Formula
  desc "Feature-rich console based todo list manager"
  homepage "https://taskwarrior.org/"
  url "https://repo.msys2.org/msys/x86_64/task-3.5.0-2-x86_64.pkg.tar.zst"
  sha256 "7efaa6389e242b0062d3325df6f061603af3439d8a2b150b16306583c49672c5"
  license "MIT"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/GothenburgBitFactory/taskwarrior/releases/download/v3.5.0/task-3.5.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
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

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
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

  resource "libp11-kit" do
    url "https://repo.msys2.org/msys/x86_64/libp11-kit-0.26.5-1-x86_64.pkg.tar.zst"
    sha256 "62707348cea67bf4ee009dbf25548c84e9f06a76b688590bd14e76321d58e125"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
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

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/task.exe", "task"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/task --version 2>&1")
  end
end
