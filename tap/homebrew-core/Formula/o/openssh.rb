class Openssh < Formula
  desc "OpenBSD freely-licensed SSH connectivity tools"
  homepage "https://www.openssh.com/"
  url "https://repo.msys2.org/msys/x86_64/openssh-10.6p1-1-x86_64.pkg.tar.zst"
  sha256 "40694d9506e733f55262c4123cf6107dd28d27457a2b320f2866cdbde454c448"
  license "SSH-OpenSSH"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://ftp.openbsd.org/pub/OpenBSD/OpenSSH/portable/"
    regex(/href=.*?openssh[._-]v?(\d+(?:\.\d+)+(?:p\d+)?)\.t/i)
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "heimdal" do
    url "https://repo.msys2.org/msys/x86_64/heimdal-7.8.0-7-x86_64.pkg.tar.zst"
    sha256 "c1e9dc49b6eaffd145abd73c8b516acca89eac64fa90031e116ac93c3b493a1e"
  end

  resource "heimdal-libs" do
    url "https://repo.msys2.org/msys/x86_64/heimdal-libs-7.8.0-7-x86_64.pkg.tar.zst"
    sha256 "badbf9d464fe8498668451b8d53cf84c7ec3bd0e7bcc0ea158bf6339a30a2416"
  end

  resource "libcbor" do
    url "https://repo.msys2.org/msys/x86_64/libcbor-0.14.0-2-x86_64.pkg.tar.zst"
    sha256 "a661826edc2d452f8d74bae2cd5974dfbcd84e23bfbfa7ea3db38ee010d7b004"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libedit" do
    url "https://repo.msys2.org/msys/x86_64/libedit-20240808_3.1-1-x86_64.pkg.tar.zst"
    sha256 "aada8ba7dfe41fdcac28d3706c0dc7c621b7667035a5e629cb5f3af7fb3f7de0"
  end

  resource "libfido2" do
    url "https://repo.msys2.org/msys/x86_64/libfido2-1.17.0-1-x86_64.pkg.tar.zst"
    sha256 "30794db36fa21af67c7126a221e2cb95b177be08845bb38720a8c9f28c39cdbb"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/scp.exe", "scp"
    msys2_bin "usr/bin/sftp.exe", "sftp"
    msys2_bin "usr/bin/ssh-add.exe", "ssh-add"
    msys2_bin "usr/bin/ssh-agent.exe", "ssh-agent"
    msys2_bin "usr/bin/ssh-keygen.exe", "ssh-keygen"
    msys2_bin "usr/bin/ssh-keyscan.exe", "ssh-keyscan"
    msys2_bin "usr/bin/ssh.exe", "ssh"
    msys2_bin "usr/bin/sshd.exe", "sshd"
  end

  test do
    assert_path_exists bin/"scp.exe"
  end
end
