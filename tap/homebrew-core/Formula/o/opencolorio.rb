class Opencolorio < Formula
  desc "Color management solution geared towards motion picture production"
  homepage "https://opencolorio.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencolorio-2.6.0-3-any.pkg.tar.zst"
  sha256 "08f2519048eeb1606a167c3fad6605035697e0a7f162d903e448896fde32cea0"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/AcademySoftwareFoundation/OpenColorIO/archive/refs/tags/v2.6.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  resource "mingw-w64-ucrt-x86_64-freeglut" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freeglut-3.8.0-1-any.pkg.tar.zst"
    sha256 "b9b9212f0b7e5ba42709e153143efac410765bde9fd1ad9991bc025bf1567805"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-giflib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-giflib-6.1.3-1-any.pkg.tar.zst"
    sha256 "9e7ac8feb0e2af6e0fce3c8ead1bd7bcaf018661d08033858c1e2657a04f6f1a"
  end

  resource "mingw-w64-ucrt-x86_64-glew" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glew-2.3.1-4-any.pkg.tar.zst"
    sha256 "69cf8d2d95edc36f481d44e2a926520bd43d3aa098246e46b57abe5d11364065"
  end

  resource "mingw-w64-ucrt-x86_64-glslang" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glslang-16.6.0-1-any.pkg.tar.zst"
    sha256 "74742908b850637e4f0710cd9630f511b0def5b0f18d653cf9657197758bda82"
  end

  resource "mingw-w64-ucrt-x86_64-imath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-imath-3.2.3-3-any.pkg.tar.zst"
    sha256 "7a2e2175d5bd126c277e1eb0a059004f8750918e7dbbd447554c7b46dca34d60"
  end

  resource "mingw-w64-ucrt-x86_64-jbigkit" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jbigkit-2.1-6-any.pkg.tar.zst"
    sha256 "9e237d1041350e7436294cfa13d4bd3d1606d1f2a32ade2eb2958b4d4e79b7ad"
  end

  resource "mingw-w64-ucrt-x86_64-lcms2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lcms2-2.19.1-1-any.pkg.tar.zst"
    sha256 "144518cc80679f7bcc1d31b5dd7ec92e8737a1d8162014e30c8c1c181f47a156"
  end

  resource "mingw-w64-ucrt-x86_64-lerc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lerc-4.1.1-1-any.pkg.tar.zst"
    sha256 "c0afbf4832488d67030c6f313349cd814946c9a7bc3ae9fd54524825c9ca055d"
  end

  resource "mingw-w64-ucrt-x86_64-libdeflate" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdeflate-1.26-1-any.pkg.tar.zst"
    sha256 "17720c77bd01641a3554801cafd71d96fb7fff7427f4cc20ac9f36efb8cb3da0"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libjpeg-turbo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjpeg-turbo-3.2.0-1-any.pkg.tar.zst"
    sha256 "7c001d00df557cfee969ad43d37b9c31ae22869e8aac4091a89dc3ae6d95e11d"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libtiff" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtiff-4.7.2-1-any.pkg.tar.zst"
    sha256 "76904b28133fc3cc0a0e518846c98d3d2166fac70ed9c6184dfe5ef145e5f279"
  end

  resource "mingw-w64-ucrt-x86_64-libwebp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwebp-1.6.0-1-any.pkg.tar.zst"
    sha256 "34ff8396c403d484c8777ad49ad0fc06dfb94fadf6f14bcf84d50c45bc6fd17e"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-minizip-ng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-minizip-ng-4.2.0-1-any.pkg.tar.zst"
    sha256 "c447aa64dd15b0e542d873b4dbfb11fd501815609faf24649d96f6757f5b678e"
  end

  resource "mingw-w64-ucrt-x86_64-openexr" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openexr-3.5.0-1-any.pkg.tar.zst"
    sha256 "a320cf33e86f726615e586f3c632cddbf978e11c17f8c5eafb7e6a5ed48d84bc"
  end

  resource "mingw-w64-ucrt-x86_64-openjph" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openjph-0.32.0-1-any.pkg.tar.zst"
    sha256 "5ef96678170cf69ebb553723cf9209e61ce21f85b7afe084da7797d686e98b5c"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-spirv-tools" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spirv-tools-3~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "d31d67fe918aeee21d1e930fd96360856a60146d23094e4ae8baf071a4049aaa"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-vulkan-loader" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vulkan-loader-1~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "065076fe13d39ab97eea4740596426d5ae741b5044e48e1c9495d04835c92dbe"
  end

  resource "mingw-w64-ucrt-x86_64-xz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xz-5.8.4-1-any.pkg.tar.zst"
    sha256 "a9d771f816d8c0c5bde152ffb27a687ede91079b8e77e5b8297efa3b5a835934"
  end

  resource "mingw-w64-ucrt-x86_64-yaml-cpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-yaml-cpp-0.9.0-1-any.pkg.tar.zst"
    sha256 "4c4240f5590115ace50e3a39259a51f671dabbdf3138d13d6dc72885f0393a85"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ocioarchive.exe", "ocioarchive"
    msys2_bin "ucrt64/bin/ociobakelut.exe", "ociobakelut"
    msys2_bin "ucrt64/bin/ociocheck.exe", "ociocheck"
    msys2_bin "ucrt64/bin/ociochecklut.exe", "ociochecklut"
    msys2_bin "ucrt64/bin/ocioconvert.exe", "ocioconvert"
    msys2_bin "ucrt64/bin/ociocpuinfo.exe", "ociocpuinfo"
    msys2_bin "ucrt64/bin/ociodisplay.exe", "ociodisplay"
    msys2_bin "ucrt64/bin/ociolutimage.exe", "ociolutimage"
    msys2_bin "ucrt64/bin/ociomakeclf.exe", "ociomakeclf"
    msys2_bin "ucrt64/bin/ociomergeconfigs.exe", "ociomergeconfigs"
    msys2_bin "ucrt64/bin/ocioperf.exe", "ocioperf"
    msys2_bin "ucrt64/bin/ociowrite.exe", "ociowrite"
  end

  test do
    system bin/"ocioarchive", "--help"
  end
end
