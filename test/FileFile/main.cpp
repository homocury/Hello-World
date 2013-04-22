// test.cpp : 콘솔 응용 프로그램에 대한 진입점을 정의합니다.
//

#include "stdafx.h"
#include <Windows.h>


using namespace std;

int _tmain(int argc, _TCHAR* argv[])
{
	if (argc < 2)
	{
		wprintf(L"Usage: %s [target_file]\n", argv[0]);
	}

	for (int i = 1; i < argc; ++i)
	{
		wprintf(L"Target file is %s\n", argv[i]);

		WIN32_FIND_DATA FindFileData;
		HANDLE hFind = FindFirstFile(argv[i], &FindFileData);
		if (hFind == INVALID_HANDLE_VALUE)
		{
			return -1;
		}

		do
		{
			if (!(FindFileData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
			{
				TCHAR fullPath[MAX_PATH] = L"";
				GetFullPathName(FindFileData.cFileName, MAX_PATH, fullPath, NULL);

				wprintf(L"	%s\n", fullPath);
			}
		} while (FindNextFile(hFind, &FindFileData) != 0);

		FindClose(hFind);
	}

	return 0;
}

