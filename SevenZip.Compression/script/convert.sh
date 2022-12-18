#!/bin/bash
MY_CC='gcc'
MY_CCFLAGS='-E -D_7ZIP_ST -D_LZMA_SIZE_OPT -DNUM_BASE_PROBS=1984 -DPPMD_32BIT -DMY_CPU_LE'
MY_CXX='g++'
MY_CXXFLAGS='-E -D_7ZIP_ST -D_LZMA_SIZE_OPT -DNUM_BASE_PROBS=1984 -DPPMD_32BIT -DMY_CPU_LE'
sourceFileFilter='-path "./CPP/Windows" -o -path "./CPP/7zip/UI" -o -path "./CPP/7zip/Bundles" -o -path "./C/Util/7zipInstall" -o -path "./C/Util/7zipUninstall" -o -path "./C/Util"'
sourcePath=${1}
intermediatePath=${2}
destinationPath=${3}
cSourceFiles=$(cd ${sourcePath}; find . \( -path "./CPP/Windows" -o -path "./CPP/7zip/UI" -o -path "./CPP/7zip/Bundles" -o -path "./C/Util/7zipInstall" -o -path "./C/Util/7zipUninstall" -o -path "./C/Util" \) -prune -o \( -name "*.c" -printf '%P\n' \) )
cppSourceFiles=$(cd ${sourcePath}; find . \( -path "./CPP/Windows" -o -path "./CPP/7zip/UI" -o -path "./CPP/7zip/Bundles" -o -path "./C/Util/7zipInstall" -o -path "./C/Util/7zipUninstall" -o -path "./C/Util" \) -prune -o \( -name "*.cpp" -printf '%P\n' \) )
includeFiles=$(find ${sourcePath} -name "*.h" -printf '%P\n')
for sourceFileName in ${cSourceFiles} ${cppSourceFiles} ${includeFiles};
do
  sourceFilePath=${sourcePath}${sourceFileName}
  intermediateFileName=${sourceFileName}
  intermediateFilePath=${intermediatePath}${intermediateFileName}
  mkdir -p $(dirname ${intermediateFilePath})
  cat ${sourceFilePath}\
    | sed -r -z 's/\\\r?\n/ /g'\
    | sed -r 's/^[ \t]*#define[ \t]+HUFFMAN_SPEED_OPT[ \t\r\n]*$//g'\
    | sed -r '/__[0-9A-Z_]+_H/!s/^[ \t]*#define[ \t]+([0-9A-Z_a-z]+)[ \t\r\n]*$/-----csharp_define \1/g'\
    | sed -r 's/^[ \t]*#define[ \t]+([0-9A-Z_a-z]+)[ \t]+([ \t0-9A-Z_a-z\(\)\+\-\*\/><]+)[ \t\r\n]*$/-----csharp_constant \1 = \2;/g'\
    | sed -r 's/^[ \t]*#define[ \t]+((Inline_MatchFinder_(GetPointerToCurrentPos|GetNumAvailableBytes|ReduceOffsets))|LzmaProps_GetNumProbs|GetPosSlot[12]{0,1}|GET_PRICE_LEN|RangeEnc_GetProcessed(_sizet)[0,1]|RC_NORM|RC_BIT_PRE|RC_BIT_[01](_BASE){0,1}|GET_PRICE[a_01]{0,3})([^\r\n]*)[\r\n]*$/-----csharp_inline_func \1\6;/g'\
    | sed -r 's/^#if[ \t]*kBadRepCode[ \t]*\!=[ \t]*\([ \t]*0xC0000000[ \t]*-[ \t]*0x400\)[ \t\r\n]*$/#if 0/g'\
    | sed -r 's/^[ \t]*#include[ \t]+<.*$//g'\
    | sed -r 's/^[ \t]*#include[ \t]+"[^"]*(7zTypes|Alloc|MyCom|ICoder|IStream|MyTypes|MyWindows|OStream|StdAfx|StreamUtils)\.h".*$//g'\
    > ${intermediateFilePath}
  echo converted ${intermediateFilePath}
done

for sourceFileName in ${cSourceFiles};
do
  intermediateFileName=${sourceFileName}
  intermediateFilePath=${intermediatePath}${intermediateFileName}
  destinationFileName=$(echo ${sourceFileName} | sed -r 's/\.c$/_c.cs/')
  destinationFilePath=${destinationPath}${destinationFileName}
  mkdir -p $(dirname ${destinationFilePath})
  chmod a+r ${destinationFilePath}
  chmod a-w ${destinationFilePath}
  chmod u+w ${destinationFilePath}
  echo "using System;" > ${destinationFilePath}
  echo "using Utility;" >> ${destinationFilePath}
  echo "" >> ${destinationFilePath}
  echo "namespace SevenZip" >> ${destinationFilePath}
  echo "{" >> ${destinationFilePath}
  echo "class " $(echo ${destinationFileName} | sed -r 's/^([^\.]+)\..*$/\1/' | sed -r 's+/+_+g' ) >> ${destinationFilePath}
  echo "{" >> ${destinationFilePath}
  echo "#if false" >> ${destinationFilePath}
  ${MY_CC} ${MY_CCFLAGS} ${intermediateFilePath}\
    | sed -r -z 's/([^A-Za-z0-9_])(EXTERN_C_BEGIN|EXTERN_C_END|MY_NO_INLINE|RINOK|SResToHRESULT|NO_INLINE|MY_FORCE_INLINE|throw\(\))([^A-Za-z0-9_])/\1\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(CLzmaProb)([^A-Za-z0-9_])/\1UInt16\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(ptrdiff_t|SizeT|size_t)([^A-Za-z0-9_])/\1Int32\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(unsigned|CLzRef)([^A-Za-z0-9_])/\1UInt32\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(BoolInt)([^A-Za-z0-9_])/\1bool\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(STDMETHODIMP|HRESULT|SRes)([^A-Za-z0-9_])/\1void\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])NULL([^A-Za-z0-9_])/\1null\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])[tT][rR][uU][eE]([^A-Za-z0-9_])/\1true\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])[fF][aA][lL][sS][eE]([^A-Za-z0-9_])/\1false\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])STDMETHOD\(([A-Za-z0-9_]+)\)([^A-Za-z0-9_])/\1void \2\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])const[ \t\r\n]+(Byte|UInt16|UInt32)[ \t\r\n]*\*/\1ReadOnlyArrayPointer<\2> /g'\
    | sed -r -z 's/([^A-Za-z0-9_])(Byte|UInt16|UInt32)[ \t\r\n]*\*/\1ArrayPointer<\2> /g'\
    | sed -r -z 's/([^A-Za-z0-9_])\(UInt32\)0x([0-9A-Fa-f]+)([^0-9A-Fa-f])/\10x\2U\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])\(UInt32\)([0-9]+)([^0-9])/\1\2U\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{2}([^0-9A-Fa-f])/\1Byte.MaxValue\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{4}([^0-9A-Fa-f])/\1UInt16.MaxValue\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{8}([^0-9A-Fa-f])/\1UInt32.MaxValue\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{16}([^0-9A-Fa-f])/\1UInt64.MaxValue\2/g'\
    | sed -r 's/\(UInt32\)\(Int32\)-1/UInt32.MaxValue/g'\
    | sed -r 's/\(UInt64\)\(Int64\)-1/UInt64.MaxValue/g'\
    | sed -r 's/for[ \t]*\([ \t]*;[ \t]*;[ \t]*\)/while (true)/g'\
    | sed -r 's/-----csharp_define ([A-Za-z0-9_]+)/#define \1/g'\
    | sed -r 's@-----(csharp_constant|csharp_inline_func)([^\r\n]*)[\r\n]*$@// &@g'\
    | sed -r 's/^#.*$//g'\
    | sed -r -z 's/[ \t]+\r{0,1}\n/\r\n/g'\
    | sed -r -z 's/(\r{0,1}\n){3,}/\r\n\r\n/g'\
    >> ${destinationFilePath}
  echo "#endif" >> ${destinationFilePath}
  echo "}" >> ${destinationFilePath}
  echo "}" >> ${destinationFilePath}
  chmod a-w ${destinationFilePath}
  echo compiled ${destinationFilePath}
done

for sourceFileName in ${cppSourceFiles};
do
  intermediateFileName=${sourceFileName}
  intermediateFilePath=${intermediatePath}${intermediateFileName}
  destinationFileName=$(echo ${sourceFileName} | sed -r 's/\.cpp$/_cpp.cs/')
  destinationFilePath=${destinationPath}${destinationFileName}
  mkdir -p $(dirname ${destinationFilePath})
  chmod a+r ${destinationFilePath}
  chmod a-w ${destinationFilePath}
  chmod u+w ${destinationFilePath}
  echo "using System;" > ${destinationFilePath}
  echo "using Utility;" >> ${destinationFilePath}
  echo "" >> ${destinationFilePath}
  echo "namespace SevenZip" >> ${destinationFilePath}
  echo "{" >> ${destinationFilePath}
  echo "class " $(echo ${destinationFileName} | sed -r 's/^([^\.]+)\..*$/\1/' | sed -r 's+/+_+g' ) >> ${destinationFilePath}
  echo "{" >> ${destinationFilePath}
  echo "#if false" >> ${destinationFilePath}
  ${MY_CXX} ${MY_CXXFLAGS} ${intermediateFilePath}\
    | sed -r -z 's/([^A-Za-z0-9_])(EXTERN_C_BEGIN|EXTERN_C_END|MY_NO_INLINE|RINOK|SResToHRESULT|NO_INLINE|MY_FORCE_INLINE|throw\(\))([^A-Za-z0-9_])/\1\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(CLzmaProb)([^A-Za-z0-9_])/\1UInt16\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(ptrdiff_t|SizeT|size_t)([^A-Za-z0-9_])/\1Int32\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(unsigned|CLzRef)([^A-Za-z0-9_])/\1UInt32\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(BoolInt)([^A-Za-z0-9_])/\1bool\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])(STDMETHODIMP|HRESULT|SRes)([^A-Za-z0-9_])/\1void\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])NULL([^A-Za-z0-9_])/\1null\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])[tT][rR][uU][eE]([^A-Za-z0-9_])/\1true\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])[fF][aA][lL][sS][eE]([^A-Za-z0-9_])/\1false\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])STDMETHOD\(([A-Za-z0-9_]+)\)([^A-Za-z0-9_])/\1void \2\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])const[ \t\r\n]+(Byte|UInt16|UInt32)[ \t\r\n]*\*/\1ReadOnlyArrayPointer<\2> /g'\
    | sed -r -z 's/([^A-Za-z0-9_])(Byte|UInt16|UInt32)[ \t\r\n]*\*/\1ArrayPointer<\2> /g'\
    | sed -r -z 's/([^A-Za-z0-9_])\(UInt32\)0x([0-9A-Fa-f]+)([^0-9A-Fa-f])/\10x\2U\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])\(UInt32\)([0-9]+)([^0-9])/\1\2U\3/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{2}([^0-9A-Fa-f])/\1Byte.MaxValue\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{4}([^0-9A-Fa-f])/\1UInt16.MaxValue\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{8}([^0-9A-Fa-f])/\1UInt32.MaxValue\2/g'\
    | sed -r -z 's/([^A-Za-z0-9_])0x0{0,}[fF]{16}([^0-9A-Fa-f])/\1UInt64.MaxValue\2/g'\
    | sed -r 's/\(UInt32\)\(Int32\)-1/UInt32.MaxValue/g'\
    | sed -r 's/\(UInt64\)\(Int64\)-1/UInt64.MaxValue/g'\
    | sed -r 's/for[ \t]*\([ \t]*;[ \t]*;[ \t]*\)/while (true)/g'\
    | sed -r 's/-----csharp_define ([A-Za-z0-9_]+)/#define \1/g'\
    | sed -r 's@-----(csharp_constant|csharp_inline_func)([^\r\n]*)[\r\n]*$@// &@g'\
    | sed -r 's/^#.*$//g'\
    | sed -r -z 's/[ \t]+\r{0,1}\n/\r\n/g'\
    | sed -r -z 's/(\r{0,1}\n){3,}/\r\n\r\n/g'\
    >> ${destinationFilePath}
  echo "#endif" >> ${destinationFilePath}
  echo "}" >> ${destinationFilePath}
  echo "}" >> ${destinationFilePath}
  chmod a-w ${destinationFilePath}
  echo compiled ${destinationFilePath}
done
